namespace Neco.Test.AspNet;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Neco.AspNet.Middlewares.CompressedStaticFiles;
using Neco.Common.Concurrency;
using Neco.Test.Mocks;
using NUnit.Framework;

[TestFixture]
public class CompressedStaticFilesTests :ATest {
	private SimpleActionQueue? _simpleActionQueue;

	[MemberNotNull(nameof(_simpleActionQueue))]
	private CompressedStaticFilesMiddleware CreateMiddleware() {
		var webHostEnv = new MockWebHostEnvironment(new PhysicalFileProvider(Path.GetFullPath("./TestData")));
		var options = new CompressedStaticFilesOptions();

		_simpleActionQueue = new SimpleActionQueue(GetLogger<SimpleActionQueue>());
		return new(_ => Task.FromException(new Exception("End of pipeline reached")), webHostEnv, Options.Create(options), LoggerFactory, _simpleActionQueue);
	}
	
	private HttpContext CreateContext(String path, String method = "GET") {
		var httpContext = new DefaultHttpContext();
		httpContext.Response.Body = new MemoryStream();
		httpContext.Request.Path = path;
		httpContext.Request.Method = method;
		return httpContext;
	}

	[Test]
	public async Task ServesNonCompressedFile() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();
		HttpContext httpContext = CreateContext("/test.txt");

		await m.InvokeAsync(httpContext);
		Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
		Assert.That(httpContext.Response.Body.Length, Is.EqualTo(448));
	}

	[Test]
	public async Task ServesBrotliCompressedFile() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();

		File.Delete("./TestData/test.txt.br.csfmcache");
		HttpContext httpContext = CreateContext("/test.txt");
		httpContext.Request.Headers.AcceptEncoding = "br";

		await m.InvokeAsync(httpContext);
		Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
		Int64 firstRequestSize = httpContext.Response.Body.Length;
		Assert.That(firstRequestSize, Is.EqualTo(448));

		await _simpleActionQueue.WaitUntilEmpty();

		httpContext = CreateContext("/test.txt");
		httpContext.Request.Headers.AcceptEncoding = "br";
		await m.InvokeAsync(httpContext);
		Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
		Int64 secondRequestSize = httpContext.Response.Body.Length;
		Assert.That(secondRequestSize, Is.LessThan(firstRequestSize));
	}
}