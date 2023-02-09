namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using Microsoft.Extensions.Logging;

internal static partial class LoggerExtensions {
	[LoggerMessage(1, LogLevel.Debug, "The request cannot be served from cache because it uses the HTTP method: {Method}.", EventName = "RequestMethodNotCacheable")]
	internal static partial void RequestMethodNotCacheable(this ILogger logger, String method);

	[LoggerMessage(2, LogLevel.Debug, "The request cannot be served from cache because it contains an 'Authorization' header.", EventName = "RequestWithAuthorizationNotCacheable")]
	internal static partial void RequestWithAuthorizationNotCacheable(this ILogger logger);

	[LoggerMessage(3, LogLevel.Debug, "The request cannot be served from cache because it contains a 'no-cache' cache directive.", EventName = "RequestWithNoCacheNotCacheable")]
	internal static partial void RequestWithNoCacheNotCacheable(this ILogger logger);

	[LoggerMessage(4, LogLevel.Debug, "The request cannot be served from cache because it contains a 'no-cache' pragma directive.", EventName = "RequestWithPragmaNoCacheNotCacheable")]
	internal static partial void RequestWithPragmaNoCacheNotCacheable(this ILogger logger);

	[LoggerMessage(5, LogLevel.Debug, "Adding a minimum freshness requirement of {Duration} specified by the 'min-fresh' cache directive.", EventName = "LogRequestMethodNotCacheable")]
	internal static partial void ExpirationMinFreshAdded(this ILogger logger, TimeSpan duration);

	[LoggerMessage(6, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age for shared caches of {SharedMaxAge} specified by the 's-maxage' cache directive.", EventName = "ExpirationSharedMaxAgeExceeded")]
	internal static partial void ExpirationSharedMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge);

	[LoggerMessage(7, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. It must be revalidated because the 'must-revalidate' or 'proxy-revalidate' cache directive is specified.", EventName = "ExpirationMustRevalidate")]
	internal static partial void ExpirationMustRevalidate(this ILogger logger, TimeSpan age, TimeSpan maxAge);

	[LoggerMessage(8, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. However, it satisfied the maximum stale allowance of {MaxStale} specified by the 'max-stale' cache directive.", EventName = "ExpirationMaxStaleSatisfied")]
	internal static partial void ExpirationMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge, TimeSpan maxStale);

	[LoggerMessage(9, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive.", EventName = "ExpirationMaxAgeExceeded")]
	internal static partial void ExpirationMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan maxAge);

	[LoggerMessage(10, LogLevel.Debug, "The response time of the entry is {ResponseTime} and has exceeded the expiry date of {Expired} specified by the 'Expires' header.", EventName = "ExpirationExpiresExceeded")]
	internal static partial void ExpirationExpiresExceeded(this ILogger logger, DateTimeOffset responseTime, DateTimeOffset expired);

	[LoggerMessage(11, LogLevel.Debug, "Response is not cacheable because it does not contain the 'public' cache directive.", EventName = "ResponseWithoutPublicNotCacheable")]
	internal static partial void ResponseWithoutPublicNotCacheable(this ILogger logger);

	[LoggerMessage(12, LogLevel.Debug, "Response is not cacheable because it or its corresponding request contains a 'no-store' cache directive.", EventName = "ResponseWithNoStoreNotCacheable")]
	internal static partial void ResponseWithNoStoreNotCacheable(this ILogger logger);

	[LoggerMessage(13, LogLevel.Debug, "Response is not cacheable because it contains a 'no-cache' cache directive.", EventName = "ResponseWithNoCacheNotCacheable")]
	internal static partial void ResponseWithNoCacheNotCacheable(this ILogger logger);

	[LoggerMessage(14, LogLevel.Debug, "Response is not cacheable because it contains a 'SetCookie' header.", EventName = "ResponseWithSetCookieNotCacheable")]
	internal static partial void ResponseWithSetCookieNotCacheable(this ILogger logger);

	[LoggerMessage(15, LogLevel.Debug, "Response is not cacheable because it contains a '.Vary' header with a value of *.", EventName = "ResponseWithVaryStarNotCacheable")]
	internal static partial void ResponseWithVaryStarNotCacheable(this ILogger logger);

	[LoggerMessage(16, LogLevel.Debug, "Response is not cacheable because it contains the 'private' cache directive.", EventName = "ResponseWithPrivateNotCacheable")]
	internal static partial void ResponseWithPrivateNotCacheable(this ILogger logger);

	[LoggerMessage(17, LogLevel.Debug, "Response is not cacheable because its status code {StatusCode} does not indicate success.", EventName = "ResponseWithUnsuccessfulStatusCodeNotCacheable")]
	internal static partial void ResponseWithUnsuccessfulStatusCodeNotCacheable(this ILogger logger, Int32 statusCode);

	[LoggerMessage(21, LogLevel.Information, "The content requested has not been modified.", EventName = "NotModifiedServed")]
	internal static partial void NotModifiedServed(this ILogger logger);

	[LoggerMessage(22, LogLevel.Debug, "Serving response to {Key} from cache.", EventName = "CachedResponseServed")]
	internal static partial void CachedResponseServed(this ILogger logger, String key);

	[LoggerMessage(23, LogLevel.Information, "No cached response available for this request and the 'only-if-cached' cache directive was specified.", EventName = "GatewayTimeoutServed")]
	internal static partial void GatewayTimeoutServed(this ILogger logger);

	[LoggerMessage(24, LogLevel.Information, "No cached response available for this request.", EventName = "NoResponseServed")]
	internal static partial void NoResponseServed(this ILogger logger);

	[LoggerMessage(26, LogLevel.Information, "The response[{StatusCode}] to {Key} with {Size} has been cached for {ValidFor}.", EventName = "ResponseCached")]
	internal static partial void ResponseCached(this ILogger logger, Int32 statusCode, String key, String size, TimeSpan validFor);

	[LoggerMessage(27, LogLevel.Information, "The response could not be cached for this request.", EventName = "ResponseNotCached")]
	internal static partial void LogResponseNotCached(this ILogger logger);

	[LoggerMessage(28, LogLevel.Warning, "The response could not be cached for this request because the 'Content-Length' did not match the body length.", EventName = "responseContentLengthMismatchNotCached")]
	internal static partial void ResponseContentLengthMismatchNotCached(this ILogger logger);

	[LoggerMessage(29, LogLevel.Debug,
		"The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
		"However, the 'max-stale' cache directive was specified without an assigned value and a stale response of any age is accepted.",
		EventName = "ExpirationInfiniteMaxStaleSatisfied")]
	internal static partial void ExpirationInfiniteMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge);
	
}