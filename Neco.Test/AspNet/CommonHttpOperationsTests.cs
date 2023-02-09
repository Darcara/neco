namespace Neco.Test.AspNet;

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neco.AspNet;
using NUnit.Framework;

[TestFixture]
public class CommonHttpOperationsTests {
	private readonly EntityTagHeaderValue _etag = new("\"F8E6648E9A6A42D695DD9F05BB51E4C7\"");

	private HttpContext CreateContext(params String[] ifnoneMatch) {
		DefaultHttpContext httpContext = new();
		httpContext.Response.Body = new MemoryStream();
		httpContext.Request.Path = "/path/to/somewhere";
		httpContext.Request.Method = "GET";
		IHeaderDictionary headers = httpContext.Request.Headers;
		headers.Host = "localhost";
		headers.CacheControl = "no-cache";
		headers.Accept = "*/*";
		headers.IfModifiedSince = DateTimeOffset.UtcNow.ToString("R");

		if (ifnoneMatch.Length > 0)
			headers.IfNoneMatch = String.Join(", ", ifnoneMatch.Select(s => s.Trim('"')).Select(s => s == "*" ? "*" : $"\"{s}\""));

		return httpContext;
	}

	[Test]
	public void MissingIfNoneMatchHeader() {
		HttpContext httpContext = CreateContext();
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, _etag), Is.EqualTo(NotModifiedResult.HeaderNotPresent));
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, null), Is.EqualTo(NotModifiedResult.HeaderNotPresent));
	}

	[Test]
	public void EtagDoesNotMatch() {
		HttpContext httpContext = CreateContext("someTag");
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, _etag), Is.EqualTo(NotModifiedResult.Modified));
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, null), Is.EqualTo(NotModifiedResult.Modified));
	}

	[Test]
	public void EtagDoesNotMatchList() {
		HttpContext httpContext = CreateContext("someTag", "anotherTag");
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, _etag), Is.EqualTo(NotModifiedResult.Modified));
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, null), Is.EqualTo(NotModifiedResult.Modified));
	}
	[Test]
	public void ModifiedOnWildcard() {
		HttpContext httpContext = CreateContext("*");
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, _etag), Is.EqualTo(NotModifiedResult.NotModified));
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, null), Is.EqualTo(NotModifiedResult.NotModified));
	}

	[Test]
	public void EtagDoesMatch() {
		HttpContext httpContext = CreateContext(_etag.ToString());
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, _etag), Is.EqualTo(NotModifiedResult.NotModified));
	}

	[Test]
	public void EtagDoesMatchInList() {
		HttpContext httpContext = CreateContext("someTag", _etag.ToString(), "anotherTag");
		Assert.That(CommonHttpOperations.IfNoneMatch(httpContext.Request.Headers, _etag), Is.EqualTo(NotModifiedResult.NotModified));
	}
	
	[Test]
	public void ModifiedIfNewerDate() {
		HttpContext httpContext = CreateContext();
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, DateTimeOffset.UtcNow.AddSeconds(5)), Is.EqualTo(NotModifiedResult.Modified));
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, (DateTimeOffset?)null), Is.EqualTo(NotModifiedResult.Modified));
	}
	[Test]
	public void NotModifiedIfPastDate() {
		HttpContext httpContext = CreateContext();
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, DateTimeOffset.UtcNow.AddSeconds(-5)), Is.EqualTo(NotModifiedResult.NotModified));
	}
	
	[Test]
	public void MissingIfModifiedSinceHeader() {
		HttpContext httpContext = CreateContext();
		httpContext.Request.Headers.IfModifiedSince = StringValues.Empty;
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, DateTimeOffset.UtcNow.AddSeconds(-5)), Is.EqualTo(NotModifiedResult.HeaderNotPresent));
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, (DateTimeOffset?)null), Is.EqualTo(NotModifiedResult.HeaderNotPresent));
	}
	
	[Test]
	public void ModifiedIfDateFailsToParse() {
		HttpContext httpContext = CreateContext();
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, "invalidDate"), Is.EqualTo(NotModifiedResult.Modified));
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers, (String?)null), Is.EqualTo(NotModifiedResult.Modified));
	}
	
	[Test]
	public void ModifiedIfHeaderDateFailsToParse() {
		HttpContext httpContext = CreateContext();
		httpContext.Request.Headers.IfModifiedSince = "invalidDate";
		Assert.That(CommonHttpOperations.IfModifiedSince(httpContext.Request.Headers,  DateTimeOffset.UtcNow.AddSeconds(-5).ToString("R")), Is.EqualTo(NotModifiedResult.Modified));
	}
}