namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using Microsoft.AspNetCore.Http;

public interface ICachingPolicy {
	/// <summary>
        /// Determine whether the response caching logic should be attempted for the incoming HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if response caching logic should be attempted; otherwise <c>false</c>.</returns>
        Boolean AttemptResponseCaching(HttpContext context);

        /// <summary>
        /// Determine whether a cache lookup is allowed for the incoming HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if cache lookup for this request is allowed; otherwise <c>false</c>.</returns>
        Boolean AllowCacheLookup(HttpContext context);

        /// <summary>
        /// Determine whether storage of the response is allowed for the incoming HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if storage of the response for this request is allowed; otherwise <c>false</c>.</returns>
        Boolean AllowCacheStorage(HttpContext context);

        /// <summary>
        /// Determine whether the response received by the middleware can be cached for future requests.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <returns><c>true</c> if the response is cacheable; otherwise <c>false</c>.</returns>
        Boolean IsResponseCacheable(HttpContext context, DateTimeOffset responseDate);

        /// <summary>
        /// Determine whether the response retrieved from the response cache is fresh and can be served.
        /// </summary>
        /// <param name="context">The <see cref="ResponseCachingContext"/>.</param>
        /// <param name="cacheEntry"></param>
        /// <param name="cachedEntryAge"></param>
        /// <returns><c>true</c> if the cached entry is fresh; otherwise <c>false</c>.</returns>
        Boolean IsCachedEntryFresh(HttpContext context, CacheEntry cacheEntry, TimeSpan cachedEntryAge);
}