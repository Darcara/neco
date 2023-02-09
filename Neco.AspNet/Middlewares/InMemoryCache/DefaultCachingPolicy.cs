namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

public class DefaultCachingPolicy : ICachingPolicy {
	private readonly ILogger<DefaultCachingPolicy> _logger;

	#region Implementation of ICachingPolicy

	public DefaultCachingPolicy(ILogger<DefaultCachingPolicy> logger) {
		_logger = logger;
	}

	/// <inheritdoc />
	public Boolean AttemptResponseCaching(HttpContext context) {
		HttpRequest request = context.Request;

		// Verify the method
		if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) {
			_logger.RequestMethodNotCacheable(request.Method);
			return false;
		}

		// Verify existence of authorization headers
		if (!StringValues.IsNullOrEmpty(request.Headers.Authorization)) {
			_logger.RequestWithAuthorizationNotCacheable();
			return false;
		}

		return true;
	}

	/// <inheritdoc />
	public Boolean AllowCacheLookup(HttpContext context) {
		IHeaderDictionary requestHeaders = context.Request.Headers;
		StringValues cacheControl = requestHeaders.CacheControl;

		// Verify request cache-control parameters
		if (!StringValues.IsNullOrEmpty(cacheControl)) {
			if (HeaderUtilities.ContainsCacheDirective(cacheControl, CacheControlHeaderValue.NoCacheString)) {
				_logger.RequestWithNoCacheNotCacheable();
				return false;
			}
		} else {
			// Support for legacy HTTP 1.0 cache directive
			if (HeaderUtilities.ContainsCacheDirective(requestHeaders.Pragma, CacheControlHeaderValue.NoCacheString)) {
				_logger.RequestWithPragmaNoCacheNotCacheable();
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public Boolean AllowCacheStorage(HttpContext context) {
		return !HeaderUtilities.ContainsCacheDirective(context.Request.Headers.CacheControl, CacheControlHeaderValue.NoStoreString);
	}

	/// <inheritdoc />
	public Boolean IsResponseCacheable(HttpContext context, DateTimeOffset responseDate) {
		StringValues responseCacheControlHeader = context.Response.Headers.CacheControl;

		// Only cache pages explicitly marked with public
		if (!HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PublicString)) {
			_logger.ResponseWithoutPublicNotCacheable();
			return false;
		}

		// Check response no-store
		if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoStoreString)) {
			_logger.ResponseWithNoStoreNotCacheable();
			return false;
		}

		// Check no-cache
		if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoCacheString)) {
			_logger.ResponseWithNoCacheNotCacheable();
			return false;
		}

		HttpResponse response = context.Response;

		// Do not cache responses with Set-Cookie headers
		if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie)) {
			_logger.ResponseWithSetCookieNotCacheable();
			return false;
		}

		// Do not cache responses varying by *
		StringValues varyHeader = response.Headers.Vary;
		if (varyHeader.Count == 1 && String.Equals(varyHeader, "*", StringComparison.OrdinalIgnoreCase)) {
			_logger.ResponseWithVaryStarNotCacheable();
			return false;
		}

		// Check private
		if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PrivateString)) {
			_logger.ResponseWithPrivateNotCacheable();
			return false;
		}

		// Check response code
		if (response.StatusCode != StatusCodes.Status200OK) {
			_logger.ResponseWithUnsuccessfulStatusCodeNotCacheable(response.StatusCode);
			return false;
		}

		// Check response freshness
		DateTimeOffset responeTime = DateTimeOffset.UtcNow;
		TimeSpan age = responeTime - responseDate;

		// Validate shared max age
		HeaderUtilities.TryParseSeconds(response.Headers.CacheControl, CacheControlHeaderValue.SharedMaxAgeString, out TimeSpan? responseSharedMaxAge);
		if (age >= responseSharedMaxAge) {
			_logger.ExpirationSharedMaxAgeExceeded(age, responseSharedMaxAge.Value);
			return false;
		}

		if (!responseSharedMaxAge.HasValue) {

			HeaderUtilities.TryParseSeconds(response.Headers.CacheControl, CacheControlHeaderValue.MaxAgeString, out TimeSpan? responseMaxAge);
			// Validate max age
			if (age >= responseMaxAge) {
				_logger.ExpirationMaxAgeExceeded(age, responseMaxAge.Value);
				return false;
			}

			if (!responseMaxAge.HasValue) {
				// Validate expiration
				if (HeaderUtilities.TryParseDate(response.Headers.Expires.ToString(), out DateTimeOffset expires) && responeTime >= expires) {
					_logger.ExpirationExpiresExceeded(responeTime, expires);
					return false;
				}
			}
		}

		return true;
	}

	/// <inheritdoc />
	public Boolean IsCachedEntryFresh(HttpContext context, CacheEntry cacheEntry, TimeSpan cachedEntryAge) {
		TimeSpan age = cachedEntryAge;
		StringValues cachedCacheControlHeaders = cacheEntry.OriginalHeaders.CacheControl;
		StringValues requestCacheControlHeaders = context.Request.Headers.CacheControl;

		// Add min-fresh requirements
		if (HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MinFreshString, out TimeSpan? minFresh)) {
			age += minFresh.Value;
			_logger.ExpirationMinFreshAdded(minFresh.Value);
		}

		// Validate shared max age, this overrides any max age settings for shared caches
		HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.SharedMaxAgeString, out TimeSpan? cachedSharedMaxAge);

		if (age >= cachedSharedMaxAge) {
			// shared max age implies must revalidate
			_logger.ExpirationSharedMaxAgeExceeded(age, cachedSharedMaxAge.Value);
			return false;
		}

		if (!cachedSharedMaxAge.HasValue) {
			HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out TimeSpan? requestMaxAge);

			HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out TimeSpan? cachedMaxAge);

			TimeSpan? lowestMaxAge = cachedMaxAge < requestMaxAge ? cachedMaxAge : requestMaxAge ?? cachedMaxAge;
			// Validate max age
			if (age >= lowestMaxAge) {
				// Must revalidate or proxy revalidate
				if (HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.MustRevalidateString)
				    || HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.ProxyRevalidateString)) {
					_logger.ExpirationMustRevalidate(age, lowestMaxAge.Value);
					return false;
				}

				Boolean maxStaleExist = HeaderUtilities.ContainsCacheDirective(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString);
				HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString, out TimeSpan? requestMaxStale);

				// Request allows stale values with no age limit
				if (maxStaleExist && !requestMaxStale.HasValue) {
					_logger.ExpirationInfiniteMaxStaleSatisfied(age, lowestMaxAge.Value);
					return true;
				}

				// Request allows stale values with age limit
				if (requestMaxStale.HasValue && age - lowestMaxAge < requestMaxStale) {
					_logger.ExpirationMaxStaleSatisfied(age, lowestMaxAge.Value, requestMaxStale.Value);
					return true;
				}

				_logger.ExpirationMaxAgeExceeded(age, lowestMaxAge.Value);
				return false;
			}

			if (!cachedMaxAge.HasValue && !requestMaxAge.HasValue) {
				// Validate expiration
				if (HeaderUtilities.TryParseDate(cacheEntry.OriginalHeaders.Expires.ToString(), out DateTimeOffset expires) && cacheEntry.Created >= expires) {
					_logger.ExpirationExpiresExceeded(cacheEntry.Created, expires);
					return false;
				}
			}
		}

		return true;
	}

	#endregion
}