namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neco.Common;

internal sealed class StaticFileInfo {
	public IFileInfo PhysicalFileInfo { get; }
	private Byte[]? _compressedBrotliResponse;
	private Int32 _compressionStarted;

	public StaticFileInfo(IFileInfo physicalFileInfo, String? contentType, Boolean isCompressible, Byte[]? compressedBrotliResponse) {
		ArgumentNullException.ThrowIfNull(physicalFileInfo);

		PhysicalFileInfo = physicalFileInfo;
		ContentType = contentType;
		IsCompressible = isCompressible;
		_compressedBrotliResponse = compressedBrotliResponse;

		DateTimeOffset last = physicalFileInfo.LastModified;
		// Truncate to the second.
		LastModified = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();

		Int64 etagHash = LastModified.ToFileTime() ^ physicalFileInfo.Length;
		Etag = new EntityTagHeaderValue('\"' + Convert.ToString(etagHash, 16) + '\"');
		_compressionStarted = compressedBrotliResponse == null ? 0 : 1;
	}

	public EntityTagHeaderValue Etag { get; }

	public String? ContentType { get; }

	public Boolean Exists => PhysicalFileInfo.Exists;

	public DateTimeOffset LastModified { get; }

	public Boolean IsCompressible { get; }

	public Int64 Length => PhysicalFileInfo.Length;

	public Task SendFileResponse(HttpContext context, CompressionMethod clientRequestedCompression) {
		ArgumentNullException.ThrowIfNull(context);

		ApplyResponseHeaders(context.Response, StatusCodes.Status200OK, clientRequestedCompression);
		if (HttpMethods.IsHead(context.Request.Method))
			return Task.CompletedTask;

		return SendFileAsync(clientRequestedCompression, context.Response, 0, Length, context.RequestAborted);
	}

	public static void SendErrorResponseHeader(HttpResponse response, Int32 httpStatusCode) {
		response.StatusCode = httpStatusCode;
		response.Headers.ContentLength = 0;

		if (httpStatusCode == StatusCodes.Status405MethodNotAllowed)
			response.Headers.Allow = new StringValues("GET, HEAD");
	}

	public Task SendHeaderResponse(HttpResponse response, Int32 statusCode, CompressionMethod clientRequestedCompression) {
		ArgumentNullException.ThrowIfNull(response);

		ApplyResponseHeaders(response, statusCode, clientRequestedCompression);
		return Task.CompletedTask;
	}

	public Boolean MarkForCompression() {
		if (_compressedBrotliResponse != null || _compressionStarted == 1) return false;
		if (Interlocked.CompareExchange(ref _compressionStarted, 1, 0) == 0)
			return true;
		return false;
	}

	public void UpdateCompressed(Byte[] compressedBrotliResponse) {
		_compressedBrotliResponse = compressedBrotliResponse;
		_compressionStarted = 1;
	}
	
	public void ResetCompressed() {
		_compressedBrotliResponse = null;
		_compressionStarted = 0;
	}

	private Task SendFileAsync(CompressionMethod clientRequestedCompression, HttpResponse response, Int64 offset, Int64 count, CancellationToken ct) {
		if (clientRequestedCompression == CompressionMethod.Brotli && _compressedBrotliResponse != null) {
			response.BodyWriter.Write(_compressedBrotliResponse);
			return Task.CompletedTask;
		}

		return SendFileAsyncCore(PhysicalFileInfo, clientRequestedCompression, response, offset, count, ct);
	}

	internal static async Task SendFileAsyncCore(IFileInfo file, CompressionMethod clientRequestedCompression, HttpResponse response, Int64 offset, Int64 count, CancellationToken ct) {
		try {
			// SequentialScan is a perf hint that requires extra sys-call on non-Windows OSes. (From: File.ReadAllBytesAsync)
			FileOptions options = FileOptions.Asynchronous | (OperatingSystem.IsWindows() ? FileOptions.SequentialScan : FileOptions.None);
			// bufferSize=1 as a workaround to indicate unbuffered read stream
			FileStream fileContent = new(file.PhysicalPath!, FileMode.Open, FileAccess.Read, FileShare.Read, 1, options);
			await using (fileContent.ConfigureAwait(false)) {
				if (offset > 0L) fileContent.Seek(offset, SeekOrigin.Begin);
				await response.StartAsync(ct).ConfigureAwait(false);

				if (clientRequestedCompression == CompressionMethod.Brotli) {
					BrotliStream outputStream = new(response.Body, CompressionLevel.Optimal, true);
					await using(outputStream.ConfigureAwait(false))
						await StreamCopyOperation.CopyToAsync(fileContent, outputStream, count, MagicNumbers.MaxNonLohBufferSize, ct).ConfigureAwait(false);
				} else if (clientRequestedCompression == CompressionMethod.Gzip) {
					GZipStream outputStream = new(response.Body, CompressionLevel.Optimal, true);
					await using(outputStream.ConfigureAwait(false))
						await StreamCopyOperation.CopyToAsync(fileContent, outputStream, count, MagicNumbers.MaxNonLohBufferSize, ct).ConfigureAwait(false);
				} else {
					await StreamCopyOperation.CopyToAsync(fileContent, response.Body, count, MagicNumbers.MaxNonLohBufferSize, ct).ConfigureAwait(false);
				}
			}
		}
		catch (OperationCanceledException) {
			// ignore
		}
		catch (FileNotFoundException) {
			if (!response.HasStarted) {
				response.Clear();
				SendErrorResponseHeader(response, StatusCodes.Status404NotFound);
			}
		}
	}

	private void ApplyResponseHeaders(HttpResponse response, Int32 statusCode, CompressionMethod clientRequestedCompression) {
		response.StatusCode = statusCode;
		Int64 contentLength = Length;
		if (clientRequestedCompression == CompressionMethod.Brotli) {
			contentLength = _compressedBrotliResponse?.Length ?? 0;
		} else if (clientRequestedCompression == CompressionMethod.Gzip) {
			contentLength = 0;
		}

		if (statusCode < 400) {
			// these headers are returned for 200, 206, and 304
			// they are not returned for 412 and 416
			if (!String.IsNullOrEmpty(ContentType)) {
				response.ContentType = ContentType;
			}

			IHeaderDictionary responseHeaders = response.Headers;
			responseHeaders[HeaderNames.LastModified] = LastModified.ToString("R");
			responseHeaders[HeaderNames.ETag] = Etag.ToString();
			responseHeaders[HeaderNames.Vary] = HeaderNames.AcceptEncoding;
			// 31536000 == 1 year
			responseHeaders[HeaderNames.CacheControl] = "public, immutable, max-age=31536000";
			// HeaderNames.Expires would be ignored due to existence of CacheControl max-age

			// This also disables re-compression from ResponseCompressionMiddleware
			if (clientRequestedCompression == CompressionMethod.Brotli) {
				responseHeaders[HeaderNames.ContentEncoding] = "br";
			} else if (clientRequestedCompression == CompressionMethod.Gzip) {
				responseHeaders[HeaderNames.ContentEncoding] = "gzip";
			}
		}

		if (statusCode == StatusCodes.Status200OK && contentLength > 0) {
			// this header is only returned here for 200
			// it already set to the returned range for 206
			// it is not returned for 304, 412, and 416
			response.ContentLength = contentLength;
		}
	}

	#region Overrides of Object

	/// <inheritdoc />
	public override String ToString() => PhysicalFileInfo.PhysicalPath ?? String.Empty;

	#endregion

	
}