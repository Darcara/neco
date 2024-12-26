namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neco.Common;
using Neco.Common.Extensions;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// <para>This cache assumes that only file system resources are being cahed. </para>
/// </remarks>
public class InMemoryCacheMiddleware {
	private readonly RequestDelegate _next;

	// see https://tools.ietf.org/html/rfc7232#section-4.1
	private static readonly String[] _headersToIncludeIn304 = ["Cache-Control", "Content-Location", "Date", "ETag", "Expires", "Vary"];

	/// The segment size for buffering the response body in bytes. The default is set to 80 KB (81920 Bytes) to avoid allocations on the LOH.
	private const Int32 _bodySegmentSize = MagicNumbers.MaxNonLohBufferSize;

	// private readonly InMemoryCacheOptions _options;
	private readonly ILogger<InMemoryCacheMiddleware> _logger;
	private readonly IMemoryCache _cache;
	private readonly ICachingPolicy _cachingPolicy;
	private readonly ObjectPool<StringBuilder> _keyBuildPool;
	private readonly Boolean _useCaseSensitivePaths;
	private readonly Int64 _maxObjectCachSize;

	public InMemoryCacheMiddleware(RequestDelegate next, IOptions<InMemoryCacheOptions> options, ObjectPoolProvider poolProvider, ILoggerFactory loggerFactory) {
		ArgumentNullException.ThrowIfNull(options);
		_next = next ?? throw new ArgumentNullException(nameof(next));
		// _options = options.Value ?? throw new ArgumentNullException(nameof(options));;
		_logger = loggerFactory.CreateLogger<InMemoryCacheMiddleware>();

		_cache = new MemoryCache(new MemoryCacheOptions() {
			SizeLimit = options.Value.SizeLimit,
			// ExpirationScanFrequency = TimeSpan.FromMinutes(1),
			// CompactionPercentage = 0.05,
		});

		_cachingPolicy = options.Value.CachingPolicy ?? new DefaultCachingPolicy(loggerFactory.CreateLogger<DefaultCachingPolicy>());
		_keyBuildPool = poolProvider.CreateStringBuilderPool();
		_useCaseSensitivePaths = options.Value.UseCaseSensitivePaths;
		_maxObjectCachSize = options.Value.MaximumBodySize;
	}

	public async Task InvokeAsync(HttpContext context) {
		// var context = new ResponseCachingContext(httpContext, _logger);

		// Should we attempt any caching logic?
		if (!_cachingPolicy.AttemptResponseCaching(context)) {
			await _next(context);
			return;
		}

		// Can this request be served from cache?
		if (_cachingPolicy.AllowCacheLookup(context) && await TryServeFromCache(context)) {
			return;
		}

		// Should we store the response to this request?
		if (_cachingPolicy.AllowCacheStorage(context)) {
			// Hook up to listen to the response stream
			BufferingStream bufferingStream = ReplaceResponseStream(context);

			try {
				await _next(context);

				DateTimeOffset responseStartTime = bufferingStream.FirstWriteTime ?? OnStartResponse(context, bufferingStream);

				// If there was no response body, check the response headers now. We can cache things like redirects.
				if (bufferingStream.IsBufferingDisabled) {
					_logger.LogResponseNotCached();
					return;
				}

				// Create the cache entry now
				HttpResponse response = context.Response;
				IHeaderDictionary headers = response.Headers;
				if (!HeaderUtilities.TryParseSeconds(headers.CacheControl, CacheControlHeaderValue.SharedMaxAgeString, out TimeSpan? responseValidFor) && !HeaderUtilities.TryParseSeconds(headers.CacheControl, CacheControlHeaderValue.MaxAgeString, out responseValidFor)) {
					if (HeaderUtilities.TryParseDate(headers.Expires.ToString(), out DateTimeOffset expires))
						responseValidFor = expires - responseStartTime;
					else
						responseValidFor = TimeSpan.FromSeconds(10);
				}

				String key = CreateCacheKey(context);
				Int64 estimatedSizeInCache = bufferingStream.Length;
				HeaderDictionary originalHeaders = new();
				foreach (KeyValuePair<String, StringValues> header in headers) {
					if (!String.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase)) {
						originalHeaders[header.Key] = header.Value;
					}
				}

				// Finalize the cache entry
				Int64? contentLength = context.Response.ContentLength;
				if (!contentLength.HasValue || contentLength == bufferingStream.Length || (bufferingStream.Length == 0 && HttpMethods.IsHead(context.Request.Method))) {
					// Add a content-length if required
					if (!response.ContentLength.HasValue && StringValues.IsNullOrEmpty(response.Headers.TransferEncoding)) {
						originalHeaders.ContentLength = bufferingStream.Length;
					}

					EntityTagHeaderValue? etag = null;
					if (!StringValues.IsNullOrEmpty(headers.ETag))
						etag = new(headers.ETag.ToString());
					HeaderUtilities.TryParseDate(headers.LastModified.ToString(), out DateTimeOffset lastModified);

					CacheEntry cacheEntry = new(responseStartTime.DateTime, response.StatusCode, originalHeaders, etag, lastModified, bufferingStream.Length, bufferingStream.Length > 0 ? bufferingStream.GetDataSegments() : null);

					_cache.Set(key, cacheEntry, new MemoryCacheEntryOptions {
						AbsoluteExpirationRelativeToNow = responseValidFor.Value,
						Size = estimatedSizeInCache,
					});
					_logger.ResponseCached(response.StatusCode, key, estimatedSizeInCache.ToFileSize(), responseValidFor.Value);
				} else {
					_logger.ResponseContentLengthMismatchNotCached();
				}
			}
			finally {
				RestoreResponseStream(context, bufferingStream.OriginalStream);
			}
		}
	}

	private async Task<Boolean> TryServeFromCache(HttpContext context) {
		String key = CreateCacheKey(context);
		Object? entry = _cache.Get(key);

		if (entry is not CacheEntry cacheEntry)
			return false;

		TimeSpan cachedEntryAge = DateTime.UtcNow - cacheEntry.Created;
		cachedEntryAge = cachedEntryAge > TimeSpan.Zero ? cachedEntryAge : TimeSpan.Zero;

		if (_cachingPolicy.IsCachedEntryFresh(context, cacheEntry, cachedEntryAge)) {
			// Check conditional request rules
			if (ContentIsNotModified(context, cacheEntry)) {
				_logger.NotModifiedServed();
				context.Response.StatusCode = StatusCodes.Status304NotModified;

				foreach (String headerToInclude in _headersToIncludeIn304) {
					if (cacheEntry.OriginalHeaders.TryGetValue(headerToInclude, out StringValues values)) {
						context.Response.Headers[headerToInclude] = values;
					}
				}
			} else {
				HttpResponse response = context.Response;
				// Copy the cached status code and response headers
				response.StatusCode = cacheEntry.StatusCode;
				foreach (KeyValuePair<String, StringValues> header in cacheEntry.OriginalHeaders) {
					response.Headers[header.Key] = header.Value;
				}

				// Note: int64 division truncates result and errors may be up to 1 second. This reduction in
				// accuracy of age calculation is considered appropriate since it is small compared to clock
				// skews and the "Age" header is an estimate of the real age of cached content.
				response.Headers.Age = HeaderUtilities.FormatNonNegativeInt64(cachedEntryAge.Ticks / TimeSpan.TicksPerSecond);

				// Copy the cached response body
				if (cacheEntry.BodyLength > 0) {
					try {
						await cacheEntry.CopyToAsync(response.BodyWriter, context.RequestAborted);
					}
					catch (OperationCanceledException) {
						context.Abort();
					}
				}

				_logger.CachedResponseServed(key);
			}

			return true;
		}

		// Only if cached, but cache could not satisfy request --> 504 as by Rfc
		if (HeaderUtilities.ContainsCacheDirective(context.Request.Headers.CacheControl, CacheControlHeaderValue.OnlyIfCachedString)) {
			_logger.GatewayTimeoutServed();
			context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
			return true;
		}

		_logger.NoResponseServed();
		return false;
	}

	private Boolean ContentIsNotModified(HttpContext context, CacheEntry cacheEntry) {
		IHeaderDictionary headers = context.Request.Headers;

		return CommonHttpOperations.IfNoneMatch(headers, cacheEntry.Etag) == NotModifiedResult.NotModified || CommonHttpOperations.IfModifiedSince(headers, cacheEntry.LastModified) == NotModifiedResult.NotModified;
	}

	private String CreateCacheKey(HttpContext context) {
		ArgumentNullException.ThrowIfNull(context);

		HttpRequest request = context.Request;
		StringBuilder builder = _keyBuildPool.Get();

		try {
			builder
				.AppendUpperInvariant(request.Method)
				.Append('\x1e')
				.AppendUpperInvariant(request.Scheme)
				.Append('\x1e')
				.AppendUpperInvariant(request.Host.Value);

			if (_useCaseSensitivePaths) {
				builder
					.Append(request.PathBase.Value)
					.Append(request.Path.Value);
			} else {
				builder
					.AppendUpperInvariant(request.PathBase.Value)
					.AppendUpperInvariant(request.Path.Value);
			}

			return builder.ToString();
		}
		finally {
			_keyBuildPool.Return(builder);
		}
	}

	internal BufferingStream ReplaceResponseStream(HttpContext context) {
		BufferingStream newResponseStream = new(
			context.Response.Body,
			_maxObjectCachSize,
			_bodySegmentSize,
			(stream) => OnStartResponse(context, stream));
		context.Response.Body = newResponseStream;

		return newResponseStream;
	}

	private DateTimeOffset OnStartResponse(HttpContext context, BufferingStream stream) {
		IHeaderDictionary headers = context.Response.Headers;
		Boolean hasDateHeaderSet = HeaderUtilities.TryParseDate(headers.Date.ToString(), out DateTimeOffset responseStartTime);
		if (!hasDateHeaderSet) responseStartTime = DateTimeOffset.UtcNow;

		if (!_cachingPolicy.IsResponseCacheable(context, responseStartTime)) {
			stream.DisableBuffering();
		} else {
			// Ensure date header is set
			if (!hasDateHeaderSet) {
				// Setting the date on the raw response headers.
				headers.Date = HeaderUtilities.FormatDate(responseStartTime);
			}
		}

		return responseStartTime;
	}

	internal static void RestoreResponseStream(HttpContext context, Stream originalStream) {
		context.Response.Body = originalStream;
	}
}