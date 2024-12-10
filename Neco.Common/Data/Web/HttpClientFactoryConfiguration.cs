namespace Neco.Common.Data.Web;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using HttpHandlerConfigurator = System.Action<System.String, System.Net.Http.SocketsHttpHandler, System.Collections.Generic.List<System.IDisposable>>;
using HttpClientConfigurator = System.Action<System.String, System.Net.Http.HttpClient>;

public class HttpClientFactoryConfiguration : IEnumerable {
	public static readonly HttpClientFactoryConfiguration Default = new() {
		KnownHttpConfigurators.HandlerDefault,
		KnownHttpConfigurators.ClientDefault,
	};

	/// <summary>
	/// Configuration actions to call for generated named handlers. The default name is <see cref="KnownClientNames.Default"/>(<see cref="string.Empty"/>). <br/>
	/// Predefined configurators are available in <see cref="KnownHttpConfigurators"/>
	/// </summary>
	/// <remarks>To apply a configuration to every created handler regardless of name, use <see cref="KnownClientNames.Always"/></remarks>
	public ConcurrentDictionary<String, List<HttpHandlerConfigurator>> HttpHandlerConfigurators { get; init; } = new();

	/// <summary>
	/// Configuration actions to call for generated named clients. The default name is <see cref="KnownClientNames.Default"/>(<see cref="string.Empty"/>). <br/>
	/// Predefined configurators are available in <see cref="KnownHttpConfigurators"/>
	/// </summary>
	/// <remarks>To apply a configuration to every created client regardless of name, use <see cref="KnownClientNames.Always"/></remarks>
	public ConcurrentDictionary<String, List<HttpClientConfigurator>> HttpClientConfigurators { get; init; } = new();

	/// <summary>
	/// Time after which a handler is marked to be disposed and a new one is created
	/// </summary>
	public TimeSpan HandlerLifetime { get; init; } = TimeSpan.FromMinutes(5);
	
	/// <summary>
	/// Time between cleanups of handlers that have exceeded their lifetime 
	/// </summary>
	public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(1);
	
	/// <summary>
	/// TRUE (default) to force a garbage collection cycle if at least one handler was disposed during cleanup
	/// </summary>
	public Boolean GarbageCollectAfterDisposedHandler { get; init; } = true;

	public HttpClientFactoryConfiguration() {
	}

	/// <param name="handlerLifetime">Time after which a handler is marked to be disposed and a new one is created</param>
	public HttpClientFactoryConfiguration(TimeSpan handlerLifetime) : this() {
		HandlerLifetime = handlerLifetime;
	}
	
	/// <param name="handlerLifetime">Time after which a handler is marked to be disposed and a new one is created</param>
	/// <param name="cleanupInterval">Time between cleanups of handlers that have exceeded their lifetime </param>
	/// <param name="garbageCollectAfterDisposedHandler">Time after which a handler is marked to be disposed and a new one is created</param>
	public HttpClientFactoryConfiguration(TimeSpan handlerLifetime, TimeSpan cleanupInterval, Boolean garbageCollectAfterDisposedHandler) :this(handlerLifetime) {
		CleanupInterval = cleanupInterval;
		GarbageCollectAfterDisposedHandler = garbageCollectAfterDisposedHandler;
	}
	
	public void Add(String name, HttpHandlerConfigurator configurator) {
		HttpHandlerConfigurators
			.GetOrAdd(name, _ => [])
			.Add(configurator);
	}

	public void Add(String name, HttpClientConfigurator configurator) {
		HttpClientConfigurators
			.GetOrAdd(name, _ => [])
			.Add(configurator);
	}

	public void Add((String? name, HttpHandlerConfigurator configurator) tpl) {
		Add(tpl.name ?? KnownClientNames.Default, tpl.configurator);
	}

	public void Add((String? name, Action<SocketsHttpHandler> configurator) tpl) {
		Add(tpl.name ?? KnownClientNames.Default, (_, handler, _) => tpl.configurator(handler));
	}

	public void Add((String? name, HttpClientConfigurator configurator) tpl) {
		Add(tpl.name ?? KnownClientNames.Default, tpl.configurator);
	}

	public void Add((String? name, Action<HttpClient> configurator) tpl) {
		Add(tpl.name ?? KnownClientNames.Default, (_, client) => tpl.configurator(client));
	}

	public void Add(HttpHandlerConfigurator configurator) => Add((KnownClientNames.Default, configurator));
	public void Add(Action<SocketsHttpHandler> configurator) => Add((KnownClientNames.Default, configurator));
	
	public void Add(HttpClientConfigurator configurator) => Add((KnownClientNames.Default, configurator));
	public void Add(Action<HttpClient> configurator) => Add((KnownClientNames.Default, configurator));

	#region Implementation of IEnumerable

	/// <inheritdoc />
	[DoesNotReturn]
	public IEnumerator GetEnumerator() => throw new InvalidOperationException();

	#endregion
};