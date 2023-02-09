namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Neco.Common.Concurrency;
using Neco.Common.Extensions;

public class StaticFileInfo : IStaticFileInfo {
	public const String CacheFileExtension = ".csfmcache";
	private readonly ILogger<StaticFileInfo> _logger;
	private readonly String _physicalPathUncompressed;
	private Int64 _brotliLength;
	private String? _physicalPathBrotli;
	private Int64 _gzipLength;
	private String? _physicalPathGzip;

	// TODO gzip
	public StaticFileInfo(IFileInfo physicalFileInfo, String? contentType, ILogger<StaticFileInfo> logger) {
		_logger = logger;
		Exists = physicalFileInfo.Exists;
		Length = physicalFileInfo.Length;
		_physicalPathUncompressed = physicalFileInfo.PhysicalPath;
		ContentType = contentType;


		DateTimeOffset last = physicalFileInfo.LastModified;
		// Truncate to the second.
		LastModified = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();

		Int64 etagHash = LastModified.ToFileTime() ^ physicalFileInfo.Length;
		Etag = new EntityTagHeaderValue('\"' + Convert.ToString(etagHash, 16) + '\"');
	}

	#region Implementation of IStaticFileInfo

	/// <inheritdoc />
	public EntityTagHeaderValue Etag { get; }

	/// <inheritdoc />
	public String? ContentType { get; }

	/// <inheritdoc />
	public Boolean Exists { get; }

	/// <inheritdoc />
	public DateTimeOffset LastModified { get; }

	/// <inheritdoc />
	public Int64 Length { get; }

	public Task SendFileResponse(HttpContext context, CompressionMethod clientRequestedCompression) {
		if (HttpMethods.IsHead(context.Request.Method)) {
			return SendHeaderResponse(context.Response, StatusCodes.Status200OK, clientRequestedCompression);
		}

		ApplyResponseHeaders(context.Response, StatusCodes.Status200OK, clientRequestedCompression);
		return SendFileAsync(context.RequestAborted, clientRequestedCompression, context.Response, 0, Length);
	}

	public Task SendHeaderResponse(HttpResponse response, Int32 statusCode, CompressionMethod clientRequestedCompression) {
		ApplyResponseHeaders(response, statusCode, clientRequestedCompression);
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public void EnsureCompression(IActionQueue actionQueue, CompressionMethod clientRequestedCompression) {
		if (clientRequestedCompression == CompressionMethod.Brotli) {
			if (_physicalPathBrotli is not null || _brotliLength == -1) return;
			if (Interlocked.Exchange(ref _brotliLength, -1) == 0) {
				// .br file already exists ?
				FileInfo providedCompressed = new(_physicalPathUncompressed + ".br");
				if (providedCompressed.Exists) {
					_logger.LogDebug("Using provided file for {Compression}: {FilePath}", CompressionMethod.Brotli, providedCompressed.FullName);
					_physicalPathBrotli = providedCompressed.FullName;
					_brotliLength = providedCompressed.Length;
					return;
				}

				// cache file already exists from before?
				FileInfo createdCompressed = new(providedCompressed.FullName + CacheFileExtension);
				if (createdCompressed.Exists) {
					_logger.LogDebug("Using existing cache file for {Compression}: {FilePath}", CompressionMethod.Brotli, createdCompressed.FullName);
					_physicalPathBrotli = createdCompressed.FullName;
					_brotliLength = createdCompressed.Length;
					return;
				}

				actionQueue.Enqueue(async (sfi, outputFile) => {
					Stopwatch sw = Stopwatch.StartNew();
					try {
						String tempFile = outputFile.FullName + ".tmp";
						if (File.Exists(tempFile)) {
							File.Delete(tempFile);
						}

						await using Stream inputStream = sfi.CreateReadStream(CompressionMethod.None);
						await using (Stream outputFileStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan)) {
							await using Stream compressedStream = new BrotliStream(outputFileStream, CompressionLevel.SmallestSize, false);
							await StreamCopyOperation.CopyToAsync(inputStream, compressedStream, sfi.Length, 65536, CancellationToken.None);
						}

						File.Move(tempFile, outputFile.FullName, true);
					}
					catch (Exception e) {
						// TODO Retry, or leave compression 'running' to disable retries ?
						Console.WriteLine(e);
					}
					finally {
						outputFile.Refresh();
						sfi._physicalPathBrotli = outputFile.FullName;
						sfi._brotliLength = outputFile.Length;
						Double reduction = 1D - outputFile.Length / (Double)sfi.Length;
						sfi._logger.LogInformation("Compressed file {FilePath} {OriginalFileSize} with {Compression} to {CompressedFileSize} in {Time} for a {ReductionPercent:P2} size reduction", sfi._physicalPathUncompressed, sfi.Length.ToFileSize(), CompressionMethod.Brotli, outputFile.Length.ToFileSize(), sw.Elapsed, reduction);
					}
				}, this, createdCompressed);
			}
		} else if (clientRequestedCompression == CompressionMethod.Gzip) {
			if (_physicalPathGzip is not null || _gzipLength == -1) return;
		} else {
			_logger.LogDebug("Unknown or unsupported compression method {Compression}", clientRequestedCompression);
		}
	}

	#endregion

	private Stream CreateReadStream(CompressionMethod clientRequestedCompression) {
		String pathToUse = _physicalPathUncompressed;
		if (clientRequestedCompression == CompressionMethod.Brotli && _physicalPathBrotli is not null)
			pathToUse = _physicalPathBrotli;
		else if (clientRequestedCompression == CompressionMethod.Gzip && _physicalPathGzip is not null)
			pathToUse = _physicalPathGzip;

		// bufferSize=1 as a workaround to indicate unbuffered read stream
		return new FileStream(pathToUse, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.Asynchronous | FileOptions.SequentialScan);
	}

	private async Task SendFileAsync(CancellationToken contextRequestAborted, CompressionMethod clientRequestedCompression, HttpResponse response, Int64 offset, Int64 count) {
		try {
			await using Stream fileContent = CreateReadStream(clientRequestedCompression);
			contextRequestAborted.ThrowIfCancellationRequested();
			if (offset > 0L)
				fileContent.Seek(offset, SeekOrigin.Begin);
			await StreamCopyOperation.CopyToAsync(fileContent, response.Body, count, 65536, contextRequestAborted);
		}
		catch (OperationCanceledException) {
			// ignore
		}
		catch (FileNotFoundException) {
			response.Clear();
			await SendHeaderResponse(response, StatusCodes.Status404NotFound, CompressionMethod.None);
		}
	}

	private void ApplyResponseHeaders(HttpResponse response, Int32 statusCode, CompressionMethod clientRequestedCompression) {
		response.StatusCode = statusCode;
		Int64 contentLength = Length;
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
			responseHeaders[HeaderNames.CacheControl] = "public, immutable, max-age=3600";
			// Would be ignored due to existence of CacheControl max-age
			// responseHeaders[HeaderNames.Expires] = DateTime.UtcNow.AddSeconds(seconds).ToString("R");

			// This also disables re-compression from ResponseCompressionMiddleware
			if (clientRequestedCompression == CompressionMethod.Brotli && _physicalPathBrotli is not null) {
				responseHeaders[HeaderNames.ContentEncoding] = "br";
				contentLength = _brotliLength;
			} else if (clientRequestedCompression == CompressionMethod.Gzip && _physicalPathGzip is not null) {
				responseHeaders[HeaderNames.ContentEncoding] = "gzip";
				contentLength = _gzipLength;
			}


			// responseHeaders[HeaderNames.AcceptRanges] = "bytes";
		}

		if (statusCode == StatusCodes.Status200OK) {
			// this header is only returned here for 200
			// it already set to the returned range for 206
			// it is not returned for 304, 412, and 416
			response.ContentLength = contentLength;
		}
	}

	#region Overrides of Object

	/// <inheritdoc />
	public override String ToString() => _physicalPathUncompressed;

	#endregion
}