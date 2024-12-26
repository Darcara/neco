namespace Neco.Test.Common.Data.Web;

using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Neco.Common.Data.Web;
using NUnit.Framework;

[TestFixture]
public class HttpClientFactoryTests : ATest {
	private CustomDisposable _testDisposable;
	HttpClientFactory _factory;

	[SetUp]
	public void Setup() {
		_testDisposable = new();
		
		HttpClientFactoryConfiguration configuration = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(100), true) {
			("TestName", KnownHttpConfigurators.HandlerDefault),
			("TestName", KnownHttpConfigurators.ClientDefault),
			KnownHttpConfigurators.DangerouslyDisableSslVerification(),
			KnownHttpConfigurators.ClientNop,
			KnownHttpConfigurators.HandlerNop,
			KnownHttpConfigurators.LikeChrome,
			KnownHttpConfigurators.WithCookies(),
			KnownHttpConfigurators.WithRateLimiting(1024),
			
			("TestName", static (_, handler, _) => handler.MaxAutomaticRedirections = 123),
			("TestName", (_, _, disposables) => disposables.Add(_testDisposable)),
			(KnownClientNames.Always, static (_, handler, _) => handler.UseCookies = true),
			
			("TestName", static (_, client) => client.DefaultRequestVersion = HttpVersion.Version30),
			(KnownClientNames.Always, static (_, client) => client.BaseAddress = new Uri("http://localhost")),
			
			("OtherName", static (HttpClient client) => throw new InvalidOperationException()),
			("OtherName", static (SocketsHttpHandler handler) => throw new InvalidOperationException()),
			(static (HttpClient client) => throw new InvalidOperationException()),
			(static (SocketsHttpHandler handler) => throw new InvalidOperationException()),
			static (_, client) => throw new InvalidOperationException(),
			static (_, handler, _) => throw new InvalidOperationException(),
		};

		_factory = new HttpClientFactory(configuration, GetLogger<HttpClientFactory>());
	}

	[Test]
	public void ProducesHandlers() {
		CreateAndVerifyHandler(_factory);
		
		Assert.That(() => {
			GC.Collect(2, GCCollectionMode.Aggressive, true);
			return _testDisposable.NumDisposeCalls;
		}, Is.Positive.After(3000, 100));
	}

	private void CreateAndVerifyHandler(IHttpMessageHandlerFactory factory) {
		using HttpMessageHandler handler = factory.CreateHandler("TestName");
		handler.Should().NotBeNull();
	}

	[Test]
	public void ProducesClients() {
		CreateAndVerifyClient(_factory);

		Assert.That(() => {
			GC.Collect(2, GCCollectionMode.Aggressive, true);
			return _testDisposable.NumDisposeCalls;
		}, Is.Positive.After(3000, 100));
	}

	// It is important that creating & disposing the client happens in a different function,
	// so the GC can collect the HttpClient  
	private static void CreateAndVerifyClient(IHttpClientFactory factory) {
		using HttpClient client = factory.CreateClient("TestName");
		client.BaseAddress.Should().Be(new Uri("http://localhost"));
		client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
		client.DefaultRequestVersion.Should().Be(HttpVersion.Version30);
	}

	[Test]
	public void Interface() {
		HttpClientFactoryConfiguration config = new() {
			HandlerLifetime = TimeSpan.Zero,
			CleanupInterval = TimeSpan.Zero,
			GarbageCollectAfterDisposedHandler = true,
			HttpClientConfigurators = [],
			HttpHandlerConfigurators = [],
		};

		Assert.Throws<InvalidOperationException>(() => config.GetEnumerator());
	}
}

internal sealed class CustomDisposable : IDisposable {
	public Int32 NumDisposeCalls {
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		get;
		private set;
	}

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		++NumDisposeCalls;
	}

	#endregion
}