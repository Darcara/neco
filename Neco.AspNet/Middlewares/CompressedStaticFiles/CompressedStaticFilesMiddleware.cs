namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Neco.Common.Concurrency;
using Neco.Common.Data;
using Neco.Common.Extensions;

/// <summary>
/// Enables serving static files for a given request path
/// </summary>
/// <remarks>
/// <para>Only brotli compressed files are cached in memory. Uncompressed or gzip compressed files are read directly from disk each time.</para>
/// <para>Since brotly is widely supported, no request for gzip is expected.</para>
/// </remarks>
public sealed partial class CompressedStaticFilesMiddleware{
	private readonly TimeProvider _timeProvider;
	private readonly ILogger<CompressedStaticFilesMiddleware> _logger;
	private readonly CompressedStaticFilesOptions _options;
	private readonly IContentTypeProvider _contentTypeProvider;
	private readonly IFileProvider _fileProvider;
	private readonly ConcurrentDictionary<String, StaticFileInfo> _knownStaticFiles = new(StringComparer.Ordinal);
	private readonly IActionQueue _actionQueue;

	/// <inheritdoc cref="CompressedStaticFilesMiddleware"/>
	public CompressedStaticFilesMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<CompressedStaticFilesOptions> options, ILogger<CompressedStaticFilesMiddleware> logger, TimeProvider? timeProvider, IActionQueue? actionQueue) {
		ArgumentNullException.ThrowIfNull(next);
		ArgumentNullException.ThrowIfNull(hostingEnv);
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logger);

		_timeProvider = timeProvider ?? TimeProvider.System;
		_logger = logger;
		_options = options.Value ?? throw new ArgumentNullException(nameof(options));
		_contentTypeProvider = _options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
		_fileProvider = _options.FileProvider ?? hostingEnv.WebRootFileProvider ?? throw new InvalidOperationException($"Missing {nameof(_options.FileProvider)}.");
		_actionQueue = actionQueue ?? new SimpleActionQueue(NullLogger<SimpleActionQueue>.Instance);
	}

	/// <inheritdoc cref="IMiddleware.InvokeAsync" />
	public Task InvokeAsync(HttpContext context) {
		ArgumentNullException.ThrowIfNull(context);
		HttpRequest request = context.Request;
		IHeaderDictionary headers = request.Headers;

		// Validation
		if (!HttpMethods.IsHead(request.Method) && !HttpMethods.IsGet(request.Method)) {
			LogRequestMethodNotSupportedMethod(request.Method);
			StaticFileInfo.SendErrorResponseHeader(context.Response, StatusCodes.Status405MethodNotAllowed);
			return Task.CompletedTask;
		}

		if (!request.Path.StartsWithSegments(_options.RequestPath, StringComparison.Ordinal, out PathString remainingRequestPath) || !remainingRequestPath.HasValue) {
			LogRequestPathPathDoesNotMatchRequestFilterFilter(request.Path, _options.RequestPath);
			StaticFileInfo.SendErrorResponseHeader(context.Response, StatusCodes.Status500InternalServerError);
			return Task.CompletedTask;
		}

		if (!TryLookupContentType(_contentTypeProvider, _options, remainingRequestPath.Value, out _)) {
			LogRequestPathPathDoesNotMatchASupportedFileType(remainingRequestPath);
			StaticFileInfo.SendErrorResponseHeader(context.Response, StatusCodes.Status415UnsupportedMediaType);
			return Task.CompletedTask;
		}

		CompressionMethod clientRequestedCompression = CommonHttpOperations.GetBestRequestedCompression(headers);

		if (!TryGetFileInfo(remainingRequestPath.Value, out StaticFileInfo? fileInfo)) {
			LogFilePathNotFound(remainingRequestPath);
			StaticFileInfo.SendErrorResponseHeader(context.Response, StatusCodes.Status404NotFound);
			return Task.CompletedTask;
		}

		if (fileInfo.IsCompressible)
			EnsureCompression(fileInfo, clientRequestedCompression);
		else
			clientRequestedCompression = CompressionMethod.None;

		LogServingPathWithCompressionFromFilepath(remainingRequestPath, clientRequestedCompression, fileInfo);
		RequestHeaders requestHeaders = request.GetTypedHeaders();

		// 14.24 If-Match
		IList<EntityTagHeaderValue> ifMatch = requestHeaders.IfMatch;
		if (ifMatch.Count > 0) {
			for (Int32 index = 0; index < ifMatch.Count; index++) {
				EntityTagHeaderValue etag = ifMatch[index];
				if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(fileInfo.Etag, useStrongComparison: true)) {
					return fileInfo.SendFileResponse(context, clientRequestedCompression);
				}
			}

			return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status412PreconditionFailed, clientRequestedCompression);
		}

		// 14.26 If-None-Match
		switch (CommonHttpOperations.IfNoneMatch(headers, fileInfo.Etag)) {
			case NotModifiedResult.NotModified: return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status304NotModified, clientRequestedCompression);
			case NotModifiedResult.Modified: return fileInfo.SendFileResponse(context, clientRequestedCompression);
		}

		DateTimeOffset now = _timeProvider.GetUtcNow();

		// 14.25 If-Modified-Since
		switch (CommonHttpOperations.IfModifiedSince(headers, fileInfo.LastModified)) {
			case NotModifiedResult.NotModified: return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status304NotModified, clientRequestedCompression);
			case NotModifiedResult.Modified: return fileInfo.SendFileResponse(context, clientRequestedCompression);
		}

		// 14.28 If-Unmodified-Since
		DateTimeOffset? ifUnmodifiedSince = requestHeaders.IfUnmodifiedSince;
		if (ifUnmodifiedSince <= now) {
			if (ifUnmodifiedSince >= fileInfo.LastModified)
				return fileInfo.SendFileResponse(context, clientRequestedCompression);
			return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status412PreconditionFailed, clientRequestedCompression);
		}

		return fileInfo.SendFileResponse(context, clientRequestedCompression);
	}

	private static Boolean TryLookupContentType(IContentTypeProvider contentTypeProvider, CompressedStaticFilesOptions options, String path, out String? contentType) {
		if (contentTypeProvider.TryGetContentType(path, out contentType)) return true;

		if (options.ServeUnknownFileTypes) {
			contentType = options.DefaultContentType;
			return true;
		}

		return false;
	}

	private Boolean TryGetFileInfo(String path, [NotNullWhen(true)] out StaticFileInfo? fileInfo) {
		if (TryGetFileInfoActual(path, out fileInfo)) return true;
		if (_options.ServeOnNotFound != null && TryGetFileInfoActual(_options.ServeOnNotFound, out fileInfo)) return true;
		return false;
	}

	private Boolean TryGetFileInfoActual(String path, [NotNullWhen(true)] out StaticFileInfo? fileInfo) {
		if (_knownStaticFiles.TryGetValue(path, out fileInfo)) return true;

		IFileInfo physicalFileInfo = _fileProvider.GetFileInfo(path);
		if (!physicalFileInfo.Exists || String.IsNullOrEmpty(physicalFileInfo.PhysicalPath)) {
			return false;
		}

		// Another request might have added this file
		fileInfo = _knownStaticFiles.GetOrAdd(path, _ => {
			TryLookupContentType(_contentTypeProvider, _options, physicalFileInfo.PhysicalPath, out String? contentType);
			Boolean assumeCompressible = _options.CompressionLookup == null || _options.CompressionLookup?.DoesFileCompress(Path.GetExtension(physicalFileInfo.PhysicalPath)) == FileCompression.Compressible;

			// .br file already exists ?
			FileInfo providedCompressed = new(physicalFileInfo.PhysicalPath + ".br");
			if (providedCompressed.Exists) {
				LogUsingProvidedFileForCompressionFilepath(CompressionMethod.Brotli, providedCompressed.FullName);
				return new StaticFileInfo(physicalFileInfo, contentType, assumeCompressible, File.ReadAllBytes(providedCompressed.FullName));
			}

			return new StaticFileInfo(physicalFileInfo, contentType, assumeCompressible, null);
		});

		return true;
	}

	[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "TopLevel worker thread")]
	private void EnsureCompression(StaticFileInfo fileInfo, CompressionMethod clientRequestedCompression) {
		if (clientRequestedCompression == CompressionMethod.None || !fileInfo.MarkForCompression())
			return;

		_actionQueue.Enqueue(static async (sfi, logger) => {
			Stopwatch sw = Stopwatch.StartNew();
			try {
				// SequentialScan is a perf hint that requires extra sys-call on non-Windows OSes. (From: File.ReadAllBytesAsync)
				FileOptions options = FileOptions.Asynchronous | (OperatingSystem.IsWindows() ? FileOptions.SequentialScan : FileOptions.None);
				// bufferSize=1 as a workaround to indicate unbuffered read/write stream
				Stream inputStream = new FileStream(sfi.PhysicalFileInfo.PhysicalPath!, FileMode.Open, FileAccess.Read, FileShare.Read, 1, options);
				Byte[] compressedData;
				await using (inputStream.ConfigureAwait(false)) {
					MemoryStream outputFileStream = new();
					Stream compressedStream = new BrotliStream(outputFileStream, CompressionLevel.SmallestSize, false);
					await using (compressedStream.ConfigureAwait(false)) {
						await StreamCopyOperation.CopyToAsync(inputStream, compressedStream, sfi.Length, 65536, CancellationToken.None).ConfigureAwait(false);
					}

					compressedData = outputFileStream.ToArray();
				}

				sfi.UpdateCompressed(compressedData);
				Double reduction = 1D - compressedData.Length / (Double)sfi.Length;
				LogCompressedFileFilepathOriginalfilesizeWithCompressionToCompressedfilesizeInTime(logger, sfi.PhysicalFileInfo.PhysicalPath, sfi.Length.ToFileSize(), CompressionMethod.Brotli, compressedData.Length.ToFileSize(), sw.Elapsed, reduction);
			}
			catch (Exception e) {
				LogFailedToCompressFilepath(logger, e, sfi.PhysicalFileInfo.PhysicalPath);
				sfi.ResetCompressed();
			}
		}, fileInfo, _logger);
	}

	[LoggerMessage(LogLevel.Trace, "Serving {path} with {compression} from {filePath}")]
	partial void LogServingPathWithCompressionFromFilepath(PathString path, CompressionMethod compression, StaticFileInfo filePath);

	[LoggerMessage(LogLevel.Trace, "File {path} not found")]
	partial void LogFilePathNotFound(PathString path);

	[LoggerMessage(LogLevel.Trace, "Request path {path} does not match a supported file type")]
	partial void LogRequestPathPathDoesNotMatchASupportedFileType(PathString path);

	[LoggerMessage(LogLevel.Warning, "Request path {path} does not match request filter {filter}")]
	partial void LogRequestPathPathDoesNotMatchRequestFilterFilter(PathString path, String filter);

	[LoggerMessage(LogLevel.Trace, "Request method not supported {method}")]
	partial void LogRequestMethodNotSupportedMethod(String method);

	[LoggerMessage(LogLevel.Debug, "Using provided file for {compression}: {filePath}")]
	partial void LogUsingProvidedFileForCompressionFilepath(CompressionMethod compression, String filePath);

	[LoggerMessage(LogLevel.Information, "Compressed file {filePath} {originalFileSize} with {compression} to {compressedFileSize} in {time} for a {reductionPercent:P2} size reduction")]
	static partial void LogCompressedFileFilepathOriginalfilesizeWithCompressionToCompressedfilesizeInTime(ILogger logger, String? filePath, String originalFileSize, CompressionMethod compression, String compressedFileSize, TimeSpan time, Double reductionPercent);

	[LoggerMessage(LogLevel.Error, "Failed to compress {filePath}")]
	static partial void LogFailedToCompressFilepath(ILogger logger, Exception e, String? filePath);
}