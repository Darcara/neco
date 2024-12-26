namespace Neco.Common.Data.Web;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using HttpHandlerConfigurator = System.Action<System.String, System.Net.Http.SocketsHttpHandler, System.Collections.Generic.List<System.IDisposable>>;
using HttpHandlerDecorator = System.Func<System.String, System.Net.Http.HttpMessageHandler, System.Net.Http.HttpMessageHandler>;
using HttpClientConfigurator = System.Action<System.String, System.Net.Http.HttpClient>;

/// <summary>
/// A factory that can create HttpClient instances with custom configuration for a given logical name
/// </summary>
public partial class HttpClientFactory : IHttpClientFactory, IHttpMessageHandlerFactory {
	private readonly HttpClientFactoryConfiguration _configuration;
	private readonly ILogger<HttpClientFactory> _logger;
	private readonly Timer _cleanupTimer;
	private readonly ConcurrentQueue<DisposableHandlerTracker> _disposableHandlers = new();
	private readonly ConcurrentDictionary<String, Lazy<ActiveHandlerTracker>> _currentHandlers = new(StringComparer.Ordinal);
	private readonly Func<String, Lazy<ActiveHandlerTracker>> _handlerFactoryDelegate;
	private readonly Action<ActiveHandlerTracker> _expireActiveHandler;
	private Int64 _numHandlersCreated;

	public HttpClientFactory(HttpClientFactoryConfiguration? configuration = null, ILogger<HttpClientFactory>? logger = null) {
		_configuration = configuration ?? HttpClientFactoryConfiguration.Default;
		_logger = logger ?? NullLogger<HttpClientFactory>.Instance;
		// This is created once at startup, so every call to CreateHandler does not allocate a new delegate
		_handlerFactoryDelegate = name => new Lazy<ActiveHandlerTracker>(() => HandlerFactory(name), LazyThreadSafetyMode.ExecutionAndPublication);
		_expireActiveHandler = ExpireActiveHandler;
		_cleanupTimer = new Timer(CleanupHandlers, null, _configuration.CleanupInterval, _configuration.CleanupInterval);
	}

	private void CleanupHandlers(Object? _) {
		Int32 numDisposed = 0;
		lock (_cleanupTimer) {
			Int32 count = _disposableHandlers.Count;
			for (Int32 i = 0; i < count; i++) {
				if (!_disposableHandlers.TryDequeue(out DisposableHandlerTracker handler)) break;
				if (handler.CanDispose) {
					handler.Dispose();
					++numDisposed;
					LogHandlerDisposed(handler.HandlerName, handler.HandlerId, handler.ClientsCreated);
				} else {
					_disposableHandlers.Enqueue(handler);
				}
			}
		}

		if (numDisposed > 0 && _configuration.GarbageCollectAfterDisposedHandler) 
			GC.Collect(2, GCCollectionMode.Forced, false, false);
	}

	private void ExpireActiveHandler(ActiveHandlerTracker activeHandler) {
		Boolean removalSuccessful = _currentHandlers.TryRemove(activeHandler.Name, out Lazy<ActiveHandlerTracker>? removedHandler);
		Debug.Assert(removalSuccessful);
		Debug.Assert(removedHandler != null && removedHandler.IsValueCreated);
		Debug.Assert(ReferenceEquals(activeHandler, removedHandler.Value));

		_disposableHandlers.Enqueue(new DisposableHandlerTracker(activeHandler));

		LogHandlerExpired(activeHandler.Name, activeHandler.Id, activeHandler.ClientsCreated);
	}

	private ActiveHandlerTracker CreateHandlerInternal(String name) {
		ActiveHandlerTracker tracker = _currentHandlers.GetOrAdd(name, _handlerFactoryDelegate).Value;
		tracker.StartExpiry(_expireActiveHandler);
		return tracker;
	}

	private ActiveHandlerTracker HandlerFactory(String name) {
		SocketsHttpHandler originalHandler = new();

		List<IDisposable> disposables = [];
		if (_configuration.HttpHandlerConfigurators.TryGetValue(KnownClientNames.Always, out List<HttpHandlerConfigurator>? configurators)) {
			for (Int32 i = 0; i < configurators.Count; i++) {
				configurators[i].Invoke(name, originalHandler, disposables);
			}
		}

		if (_configuration.HttpHandlerConfigurators.TryGetValue(name, out configurators)) {
			for (Int32 i = 0; i < configurators.Count; i++) {
				configurators[i].Invoke(name, originalHandler, disposables);
			}
		}
		
		HttpMessageHandler handler = originalHandler;
		if (_configuration.HttpHandlerDecorators.TryGetValue(KnownClientNames.Always, out List<HttpHandlerDecorator>? decorators)) {
			for (Int32 i = 0; i < decorators.Count; i++) {
				HttpMessageHandler? decoratedHandler = decorators[i].Invoke(name, handler);
				handler = decoratedHandler ?? throw new InvalidOperationException($"HttpMessageHandler decorator must not return null for {name}");
			}
		}
		
		if (_configuration.HttpHandlerDecorators.TryGetValue(name, out decorators)) {
			for (Int32 i = 0; i < decorators.Count; i++) {
				HttpMessageHandler? decoratedHandler = decorators[i].Invoke(name, handler);
				handler = decoratedHandler ?? throw new InvalidOperationException($"HttpMessageHandler decorator must not return null for {name}");
			}
		}

		ActiveHandlerTracker newHandlerTracker = new(new LifetimeTrackingHttpMessageHandlerDecorator(handler), disposables, _configuration.HandlerLifetime, name, Interlocked.Increment(ref _numHandlersCreated));
		LogHandlerCreated(newHandlerTracker.Name, newHandlerTracker.Id);
		return newHandlerTracker;
	}

	#region Implementation of IHttpClientFactory

	/// <inheritdoc />
	public HttpClient CreateClient(String name) {
		ArgumentNullException.ThrowIfNull(name);
		ActiveHandlerTracker tracker = CreateHandlerInternal(name);
		HttpClient client = new(tracker.Handler, disposeHandler: false);

		if (_configuration.HttpClientConfigurators.TryGetValue(KnownClientNames.Always, out List<HttpClientConfigurator>? configurators)) {
			for (Int32 i = 0; i < configurators.Count; i++) {
				configurators[i].Invoke(name, client);
			}
		}

		if (_configuration.HttpClientConfigurators.TryGetValue(name, out configurators)) {
			for (Int32 i = 0; i < configurators.Count; i++) {
				configurators[i].Invoke(name, client);
			}
		}

		Interlocked.Increment(ref tracker.ClientsCreated);
		return client;
	}

	#endregion

	#region Implementation of IHttpMessageHandlerFactory

	/// <inheritdoc />
	public HttpMessageHandler CreateHandler(String name) {
		ArgumentNullException.ThrowIfNull(name);
		return CreateHandlerInternal(name).Handler;
	}

	#endregion

	[LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "New HttpMessageHandler created: {name}#{id}")]
	private partial void LogHandlerCreated(String name, Int64 id);

	[LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "HttpMessageHandler {name}#{id} expired after creating {numClientsCreated} clients")]
	private partial void LogHandlerExpired(String name, Int64 id, Int64 numClientsCreated);

	[LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "HttpMessageHandler {name}#{id} disposed after creating {numClientsCreated} clients")]
	private partial void LogHandlerDisposed(String name, Int64 id, Int64 numClientsCreated);
}