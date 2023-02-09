namespace Neco.AspNet;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

public static class NotModifiedResult {
	public const Int32 HeaderNotPresent = 0;
	public const Int32 NotModified = 1;
	public const Int32 Modified = 2;
}

public static class CommonHttpOperations {
	// Slightly faster than using RequestHeaders from 'GetTypedHeaders'
	public static Int32 IfNoneMatch(IHeaderDictionary requestHeaders, EntityTagHeaderValue? etag) {
		var ifNoneMatchHeader = requestHeaders.IfNoneMatch;
		if (StringValues.IsNullOrEmpty(ifNoneMatchHeader)) return NotModifiedResult.HeaderNotPresent;

		if (ifNoneMatchHeader.Count == 1 && StringSegment.Equals(ifNoneMatchHeader[0], EntityTagHeaderValue.Any.Tag, StringComparison.OrdinalIgnoreCase)) {
			return NotModifiedResult.NotModified;
		}

		if (etag == null) return NotModifiedResult.Modified;

		if (EntityTagHeaderValue.TryParseList(ifNoneMatchHeader, out var ifNoneMatchEtags)) {
			for (var i = 0; i < ifNoneMatchEtags.Count; i++) {
				var requestETag = ifNoneMatchEtags[i];
				if (etag.Compare(requestETag, useStrongComparison: false)) {
					return NotModifiedResult.NotModified;
				}
			}
		}

		return NotModifiedResult.Modified;
	}

	public static Int32 IfModifiedSince(IHeaderDictionary requestHeaders, String? objectDateTime) {
		if (String.IsNullOrEmpty(objectDateTime)) return IfModifiedSince(requestHeaders, (DateTimeOffset?)null);

		if (HeaderUtilities.TryParseDate(objectDateTime, out DateTimeOffset modified))
			return IfModifiedSince(requestHeaders, modified);
		return NotModifiedResult.Modified;
	}

	public static Int32 IfModifiedSince(IHeaderDictionary requestHeaders, DateTimeOffset? objectDateTime) {
		var ifModifiedSince = requestHeaders.IfModifiedSince;
		if (StringValues.IsNullOrEmpty(ifModifiedSince)) return NotModifiedResult.HeaderNotPresent;

		if (objectDateTime == null) return NotModifiedResult.Modified;
		if (HeaderUtilities.TryParseDate(ifModifiedSince.ToString(), out DateTimeOffset modifiedSince) && objectDateTime <= modifiedSince) {
			return NotModifiedResult.NotModified;
		}

		return NotModifiedResult.Modified;
	}
}