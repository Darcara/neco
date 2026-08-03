namespace Neco.Test.AspNet;

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neco.AspNet.Middlewares.CompressedStaticFiles;
using Neco.Common;
using Neco.Common.Concurrency;
using Neco.Common.Data;
using Neco.Common.Extensions;
using Neco.Test.Mocks;

[TestFixture]
internal class CompressedStaticFilesTests : ATest {
	private SimpleActionQueue? _simpleActionQueue;

	[MemberNotNull(nameof(_simpleActionQueue))]
	private CompressedStaticFilesMiddleware CreateMiddleware(Boolean serveUnknownFiles = false, Func<IFileInfo, Stream, Stream, Int64, CancellationToken, Task>? mutate = null) {
		MockWebHostEnvironment webHostEnv = new(new PhysicalFileProvider(Path.GetFullPath("./TestData")));
		CompressedStaticFilesOptions options = new() {
			CompressionLookup = StaticFileCompressionLookup.Instance,
			ServeUnknownFileTypes = serveUnknownFiles,
			MutateFile = mutate,
		};

		_simpleActionQueue = new SimpleActionQueue(GetLogger<SimpleActionQueue>());
		return new(_ => Task.FromException(new Exception("End of pipeline reached")), webHostEnv, Options.Create(options), LoggerFactory.CreateLogger<CompressedStaticFilesMiddleware>(), TimeProvider.System, _simpleActionQueue);
	}

	private HttpContext CreateContext(String path, String? method = null) {
		DefaultHttpContext httpContext = new();
		httpContext.Response.Body = new MemoryStream();
		httpContext.Request.Path = path;
		httpContext.Request.Method = method ?? HttpMethods.Get;
		return httpContext;
	}

	[Test]
	public void ShowTestFileSizes() {
		Byte[] bytes = File.ReadAllBytes("TestData/test.txt");
		Logger.LogInformation("Uncompressed Bytes: {Length}", bytes.Length);
		for (Int32 i = 0; i < 12; ++i) {
			MemoryStream memoryStream = new();
			using (BrotliStream brStream = new(memoryStream, new BrotliCompressionOptions() { Quality = i }, true)) {
				brStream.Write(bytes);
			}

			Logger.LogInformation("Brotli level {BrotliCompressionLevel,2} = compressed Bytes: {FileSize}", i, memoryStream.Position.ToFileSize());
		}

		for (Int32 i = -1; i < 10; ++i) {
			MemoryStream memoryStream = new();
			using (GZipStream gzipStream = new(memoryStream, new ZLibCompressionOptions() { CompressionLevel = i }, true)) {
				gzipStream.Write(bytes);
			}

			Logger.LogInformation("GZip level {BrotliCompressionLevel,2} = compressed Bytes: {FileSize}", i, memoryStream.Position.ToFileSize());
		}
	}

	[Test]
	public async Task MutatesFiles() {
		CompressedStaticFilesMiddleware m = CreateMiddleware(true, ReplacePlaceHolder);

		async Task ReplacePlaceHolder(IFileInfo fileInfoi, Stream inputStream, Stream outputStream, Int64 count, CancellationToken token) {
			String fileContent = await new StreamReader(inputStream, leaveOpen: true).ReadToEndAsync(token);
			fileContent = fileContent.Replace("%PLACEHOLDER%", String.Empty);
			StreamWriter streamWriter = new(outputStream, encoding: MagicNumbers.Utf8NoBom, leaveOpen: true);
			await using (streamWriter.ConfigureAwait(false)) {
				await streamWriter.WriteAsync(fileContent).ConfigureAwait(false);
			}
		}

		// First run from disk
		{
			HttpContext httpContext = CreateContext("/someTextFileWithPlaceholders.txt");
			httpContext.Request.Headers.AcceptEncoding = "br";

			await m.InvokeAsync(httpContext);
			using (Assert.EnterMultipleScope()) {
				Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
				Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
			}
			
			httpContext.Response.Body.Position = 0;
			await using BrotliStream decompressionStream = new(httpContext.Response.Body, CompressionMode.Decompress, leaveOpen: true);
			String content = await new StreamReader(decompressionStream, leaveOpen: true).ReadToEndAsync();

			Assert.That(content, Does.Not.Contain("%"));
		}

		await _simpleActionQueue.WaitUntilEmpty();
		// again to test memory compressed
		{
			HttpContext httpContext = CreateContext("/someTextFileWithPlaceholders.txt");
			httpContext.Request.Headers.AcceptEncoding = "br";

			await m.InvokeAsync(httpContext);
			using (Assert.EnterMultipleScope()) {
				Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
				Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
			}

			httpContext.Response.Body.Position = 0;
			await using BrotliStream decompressionStream = new(httpContext.Response.Body, CompressionMode.Decompress, leaveOpen: true);
			String content = await new StreamReader(decompressionStream, leaveOpen: true).ReadToEndAsync();

			Assert.That(content, Does.Not.Contain("%"));
		}
	}

	[Test]
	public async Task ServesIncompressibleFileNonCompressed() {
		CompressedStaticFilesMiddleware m = CreateMiddleware(true);
		HttpContext httpContext = CreateContext("/someTextFile.txt.br");
		httpContext.Request.Headers.AcceptEncoding = "br";

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
			Assert.That(httpContext.Response.Body.Length, Is.EqualTo(3));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Is.Empty);
		}
	}

	[Test]
	public async Task ServesNonCompressedFile() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();
		HttpContext httpContext = CreateContext("/test.txt");

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
			Assert.That(httpContext.Response.Body.Length, Is.EqualTo(2168));
		}
	}

	[Test]
	public async Task ServesGzipCompressedFile() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();
		HttpContext httpContext = CreateContext("/test.txt");
		httpContext.Request.Headers.AcceptEncoding = "gzip";

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("gzip"));
			Assert.That(httpContext.Response.Body.Length, Is.EqualTo(971));
		}
	}

	[Test]
	public async Task ServesBrotliCompressedFile() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();

		FileInfo oldCacheFile = new("./TestData/test.txt.csfmcache");
		if (oldCacheFile.Exists) {
			oldCacheFile.Attributes = FileAttributes.Normal;
			oldCacheFile.Delete();
		}

		HttpContext httpContext = CreateContext("/test.txt");
		httpContext.Request.Headers.AcceptEncoding = "gzip, br";

		await m.InvokeAsync(httpContext);
		Int64 firstRequestSize = httpContext.Response.Body.Length;
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
			Assert.That(firstRequestSize, Is.EqualTo(988));
		}

		await _simpleActionQueue.WaitUntilEmpty();

		httpContext = CreateContext("/test.txt");
		httpContext.Request.Headers.AcceptEncoding = "br";
		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
			Int64 secondRequestSize = httpContext.Response.Body.Length;
			Assert.That(secondRequestSize, Is.LessThan(firstRequestSize));
		}
	}

	[Test]
	public async Task Status404OnMissingFile() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();
		HttpContext httpContext = CreateContext("/IDoNotExist.txt");
		httpContext.Request.Headers.AcceptEncoding = "br";

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.NotFound));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Is.Empty);
		}
	}

	[Test]
	public async Task Status415OnUnknownFileType() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();
		HttpContext httpContext = CreateContext("/IDoNotExist.txt.br");
		httpContext.Request.Headers.AcceptEncoding = "br";

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.UnsupportedMediaType));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Is.Empty);
		}
	}

	[Test]
	public async Task Status304IfNotModified() {
		CompressedStaticFilesMiddleware m = CreateMiddleware();
		HttpContext httpContext = CreateContext("/test.txt");
		httpContext.Request.Headers.AcceptEncoding = "br";
		httpContext.Request.Headers.IfModifiedSince = DateTime.UtcNow.AddDays(1).ToString("R");

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.NotModified));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
			Assert.That(httpContext.Response.Headers.ContentLength, Is.Null);
			Assert.That(httpContext.Response.Body.Length, Is.Zero);
		}
	}

	[Test]
	public async Task DoesNotServeCacheFiles() {
		CompressedStaticFilesMiddleware m = CreateMiddleware(true);
		HttpContext httpContext = CreateContext("/test.txt.csfmcache");
		httpContext.Request.Headers.AcceptEncoding = "br";

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.NotFound));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Is.Empty);
		}
	}

	[Test]
	public async Task GivesHeadWhenAsked() {
		CompressedStaticFilesMiddleware m = CreateMiddleware(true);
		HttpContext httpContext = CreateContext("/test.txt", HttpMethods.Head);
		httpContext.Request.Headers.AcceptEncoding = "br";

		await m.InvokeAsync(httpContext);
		using (Assert.EnterMultipleScope()) {
			Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
			Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
			Assert.That(httpContext.Response.Headers.ContentLength, Is.Null);
			Assert.That(httpContext.Response.Body.Length, Is.Zero);
		}

		// httpContext = CreateContext("/someTextFile.txt");
		// httpContext.Request.Headers.AcceptEncoding = "br";
		//
		// await m.InvokeAsync(httpContext);
		// using (Assert.EnterMultipleScope()) {
		// 	Assert.That(httpContext.Response.StatusCode, Is.EqualTo((Int32)HttpStatusCode.OK));
		// 	Assert.That(httpContext.Response.Headers.ContentEncoding, Contains.Item("br"));
		// 	Assert.That(httpContext.Response.Headers.ContentLength, Is.Not.Null);
		// 	Assert.That(httpContext.Response.Headers.ContentLength, Is.GreaterThan(0));
		// 	Assert.That(httpContext.Response.Body.Length, Is.Zero);
		// }
	}
}