namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Don't use this. Will cache everything and disregard any caching directives. 
/// </summary>
public class AlwaysCachingPolicy : ICachingPolicy{
	#region Implementation of ICachingPolicy

	/// <inheritdoc />
	public Boolean AttemptResponseCaching(HttpContext context) => true;

	/// <inheritdoc />
	public Boolean AllowCacheLookup(HttpContext context) => true;

	/// <inheritdoc />
	public Boolean AllowCacheStorage(HttpContext context) => true;

	/// <inheritdoc />
	public Boolean IsResponseCacheable(HttpContext context, DateTimeOffset responseDate) => true;

	/// <inheritdoc />
	public Boolean IsCachedEntryFresh(HttpContext context, CacheEntry cacheEntry, TimeSpan cachedEntryAge) => true;

	#endregion
}