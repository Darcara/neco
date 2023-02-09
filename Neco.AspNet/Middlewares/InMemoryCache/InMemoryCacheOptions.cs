namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;

/// <summary>
/// Options for configuring the <see cref="InMemoryCacheMiddleware"/>.
/// </summary>
public class InMemoryCacheOptions {
	/// <summary>
	/// The size limit for the response cache middleware in bytes. The default is set to 100 MB.
	/// When this limit is exceeded, no new responses will be cached until older entries are
	/// evicted.
	/// </summary>
	public Int64 SizeLimit { get; set; } = 100 * 1024 * 1024;

	/// <summary>
	/// The largest cacheable size for the response body in bytes. The default is set to 5 MB.
	/// If the response body exceeds this limit, it will not be cached by the <see cref="InMemoryCacheMiddleware"/>.
	/// </summary>
	public Int64 MaximumBodySize { get; set; } = 5 * 1024 * 1024;

	/// <summary>
	/// <c>true</c> if request paths are case-sensitive; otherwise <c>false</c>. The default is to treat paths as case-sensitive.
	/// </summary>
	public Boolean UseCaseSensitivePaths { get; set; } = true;
	
	public ICachingPolicy? CachingPolicy { get; set; }
}