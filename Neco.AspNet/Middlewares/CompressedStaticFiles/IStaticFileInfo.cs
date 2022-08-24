namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Neco.Common.Concurrency;

public interface IStaticFileInfo {
	public EntityTagHeaderValue Etag { get; }
	String? ContentType { get; }
	Boolean Exists { get; }

	Int64 Length { get; }

	DateTimeOffset LastModified { get; }

	// Task Serve(RequestDelegate next, HttpContext context, CompressionMethod clientRequestedCompression);
	Task SendFileResponse(HttpContext context, CompressionMethod clientRequestedCompression);
	Task SendHeaderResponse(HttpResponse response, Int32 statusCode, CompressionMethod clientRequestedCompression);
	void EnsureCompression(IActionQueue actionQueue, CompressionMethod clientRequestedCompression);
}