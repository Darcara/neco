namespace Neco.Common.Data.Web;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public static partial class KnownHttpConfigurators {
	public static Action<String, SocketsHttpHandler, List<IDisposable>> HandlerNop { get; } = static (_, _, _) => { };
	public static Action<String, HttpClient> ClientNop { get; } = static (_, _) => { };

	public static Action<String, SocketsHttpHandler, List<IDisposable>> HandlerDefault { get; } = static (_, handler, _) => {
		// See HttpHandlerDefaults for defaults
		handler.UseCookies = false; /* default: true */
		handler.UseProxy = false;
		handler.ConnectTimeout = TimeSpan.FromSeconds(15); /* default: Timeout.Infinite */
		handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(10); /* default: 1 minute */
		handler.PooledConnectionLifetime = TimeSpan.FromMinutes(1); /* default: Timeout.Infinite */
		handler.AutomaticDecompression = DecompressionMethods.All; /* default: None */
	};

	public static Action<String, HttpClient> ClientDefault { get; } =
		static (_, client) => {
			client.Timeout = TimeSpan.FromSeconds(30); // default = 100 seconds

			// Default is HTTP 1.1 or lower
			client.DefaultRequestVersion = HttpVersion.Version11;
			client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
		};

	public static Action<String, HttpClient> LikeChrome { get; } =
		static (_, client) => {
			// a few default headers from chrome 131
			client.DefaultRequestHeaders.AcceptLanguage.Add(StringWithQualityHeaderValue.Parse("en-US"));
			client.DefaultRequestHeaders.AcceptLanguage.Add(StringWithQualityHeaderValue.Parse("en;q=0.9"));

			client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
			client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));
			client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("br"));
			// chrome would add zstd, but dotnet does not support that

			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
			client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
		};

	public static Action<String, SocketsHttpHandler, List<IDisposable>> WithCookies(params Cookie[] cookies) {
		return (_, handler, _) => {
			handler.UseCookies = true;
			handler.CookieContainer = new CookieContainer();
			foreach (Cookie cookie in cookies) {
				handler.CookieContainer.Add(cookie);
			}
		};
	}

	public static Action<String, SocketsHttpHandler, List<IDisposable>> DangerouslyDisableSslVerification() => DangerouslyDisableSslVerification(NullLogger.Instance);

	public static Action<String, SocketsHttpHandler, List<IDisposable>> DangerouslyDisableSslVerification(ILogger logger) {
		return (id, handler, _) => {
			handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, errors) => {
				if (errors != SslPolicyErrors.None)
					LogSslErrorIgnored(logger, id, errors);
				return true;
			};
		};
	}

	public static Action<String, SocketsHttpHandler, List<IDisposable>> WithRateLimiting(RateLimiter? rateLimiter) {
		return (_, handler, _) => {
			if (rateLimiter == null) return;
			handler.ConnectCallback = (ctx, token) => CreateRateLimitedStream(rateLimiter, ctx, token);
		};
	}

	public static Action<String, SocketsHttpHandler, List<IDisposable>> WithRateLimiting(Int32 maxIncomingBytesPerSecond) {
		return (_, handler, disposables) => {
			if (maxIncomingBytesPerSecond <= 0 || maxIncomingBytesPerSecond >= Int32.MaxValue) return;

			TokenBucketRateLimiter rateLimiter = new(new TokenBucketRateLimiterOptions {
				AutoReplenishment = true,
				QueueLimit = Int32.MaxValue,
				ReplenishmentPeriod = TimeSpan.FromMilliseconds(50),
				TokenLimit = Math.Max(1, maxIncomingBytesPerSecond),
				TokensPerPeriod = Math.Max(1, maxIncomingBytesPerSecond / 20),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
			});

			handler.ConnectCallback = (ctx, token) => CreateRateLimitedStream(rateLimiter, ctx, token);

			disposables.Add(rateLimiter);
		};
	}

	private static async ValueTask<Stream> CreateRateLimitedStream(RateLimiter rateLimiter, SocketsHttpConnectionContext ctx, CancellationToken cancellationToken) {
		// From HttpConnectionPool	
		Socket socket = new(SocketType.Stream, ProtocolType.Tcp) {
			NoDelay = true,
			// ToDo: Validate  -->
			LingerState = new LingerOption(false, 0),
			SendTimeout = 30000,
			ReceiveTimeout = 30000,
			// <---
		};

		Stream stream;
		try {
			await socket.ConnectAsync(ctx.DnsEndPoint, cancellationToken).ConfigureAwait(false);

			stream = new NetworkStream(socket, ownsSocket: true);
		}
		catch {
			socket.Dispose();
			throw;
		}

		return new RateLimitedStream(stream, rateLimiter, disposeStream: true, disposeReadRateLimiter: false);
	}

	[LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Ignoring SSL errors for client '{clientId}': {errors}")]
	private static partial void LogSslErrorIgnored(ILogger logger, String clientId, SslPolicyErrors errors);
}