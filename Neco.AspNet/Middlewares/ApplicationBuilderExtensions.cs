// ReSharper disable once CheckNamespace

namespace Microsoft.AspNetCore.Builder;

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Neco.AspNet;
using Neco.AspNet.Middlewares.CompressedStaticFiles;
using Neco.AspNet.Middlewares.InMemoryCache;

public static class ApplicationBuilderExtensions {
	public static IApplicationBuilder UseCompressedStaticFiles(this IApplicationBuilder app, CompressedStaticFilesOptions options) {
		ArgumentNullException.ThrowIfNull(app);

		return app.UseMiddleware<CompressedStaticFilesMiddleware>(Options.Create(options));
	}

	public static IApplicationBuilder UseCompressedStaticFiles(this IApplicationBuilder app) => UseCompressedStaticFiles(app, new CompressedStaticFilesOptions());

	public static IApplicationBuilder UseInMemoryCache(this IApplicationBuilder app, InMemoryCacheOptions options) {
		ArgumentNullException.ThrowIfNull(app);

		return app.UseMiddleware<InMemoryCacheMiddleware>(Options.Create(options));
	}

	public static IApplicationBuilder UseInMemoryCache(this IApplicationBuilder app) => UseInMemoryCache(app, new InMemoryCacheOptions());

	public static IApplicationBuilder RunSingleFileWhen(this IApplicationBuilder app, Func<HttpContext, Boolean> predicate, String file, SingleFileServeOptions options = SingleFileServeOptions.Uncachable | SingleFileServeOptions.Compress) {
		app.MapWhen(predicate, configure => configure.Run(context => ServeSingleFile(context, file, options)));
		return app;
	}

	private static Task ServeSingleFile(HttpContext context, String file, SingleFileServeOptions options) {
		PhysicalFileInfo fileinfo = new(new FileInfo(file));
		if (!fileinfo.Exists) {
			context.Response.StatusCode = 404;
			context.Response.ContentLength = 0;
			return Task.CompletedTask;
		}

		IHeaderDictionary responseHeaders = context.Response.Headers;
		if (options.HasFlag(SingleFileServeOptions.Uncachable)) {
			responseHeaders[HeaderNames.Vary] = "*";
			responseHeaders[HeaderNames.CacheControl] = "private, no-cache, no-store, max-age=0, must-revalidate";
			// deprecated header icluded for compatibility
			responseHeaders[HeaderNames.Pragma] = "no-cache";
		} else if (options.HasFlag(SingleFileServeOptions.Cachable)) {
			Int64 etagHash = fileinfo.LastModified.ToFileTime() ^ fileinfo.Length;
			responseHeaders[HeaderNames.ETag] = '"' + Convert.ToString(etagHash, 16) + '"';
			responseHeaders[HeaderNames.LastModified] = fileinfo.LastModified.ToString("R");
			responseHeaders[HeaderNames.Vary] = HeaderNames.AcceptEncoding;
			responseHeaders[HeaderNames.CacheControl] = "public, immutable, max-age=31536000";
		}

		if (!options.HasFlag(SingleFileServeOptions.Compress) || fileinfo.Length < 500)
			return context.Response.SendFileAsync(fileinfo, context.RequestAborted);

		CompressionMethod compression = CommonHttpOperations.GetBestRequestedCompression(context.Request.Headers);
		if (compression == CompressionMethod.Brotli) {
			responseHeaders[HeaderNames.ContentEncoding] = "br";
		} else if (compression == CompressionMethod.Gzip) {
			responseHeaders[HeaderNames.ContentEncoding] = "gzip";
		}

		return StaticFileInfo.SendFileAsyncCore(fileinfo, compression, null, context.Response, context.RequestAborted);
	}

	/// <inheritdoc cref="WebSocketMiddlewareExtensions.UseWebSockets(Microsoft.AspNetCore.Builder.IApplicationBuilder,WebSocketOptions)"/>
	/// <param name="enableCompression"><see langword="true"/> (default) to enable websocket per frame deflate compression</param>
	/// <remarks>
	/// Forces WebSocket Compression, use instead of <see cref="WebSocketMiddlewareExtensions.UseWebSockets(Microsoft.AspNetCore.Builder.IApplicationBuilder)">UseWebSockets</see>. Place before SignalR <c>MapHub</c> or <c>UseSrTsNetLayer</c>.
	/// </remarks>
	public static IApplicationBuilder UseCompressibleWebSockets(this IApplicationBuilder builder, Boolean enableCompression = true, WebSocketOptions? options = null) {
		if (options == null) {
			builder.UseWebSockets();
		} else {
			builder.UseWebSockets(options);
		}

		if (enableCompression) {
			builder.Use((context, next) => {
				if (context.WebSockets.IsWebSocketRequest &&
				    context.Features.Get<IHttpWebSocketFeature>() is { } currentFeature) {
					context.Features.Set<IHttpWebSocketFeature>(new ForceWebsocketCompression(currentFeature));
				}

				return next(context);
			});
		}

		return builder;
	}
}

/// <inheritdoc/>
file sealed class ForceWebsocketCompression(IHttpWebSocketFeature originalFeature) : IHttpWebSocketFeature {
	/// <inheritdoc/>
	public Boolean IsWebSocketRequest => originalFeature.IsWebSocketRequest;

	/// <inheritdoc/>
	public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) {
		context.DangerousEnableCompression = true;
		// .ServerMaxWindowBits is already 15, best compression, most memory
		// .DisableServerContextTakeover is false by default for best compression, most memory
		return originalFeature.AcceptAsync(context);
	}
}