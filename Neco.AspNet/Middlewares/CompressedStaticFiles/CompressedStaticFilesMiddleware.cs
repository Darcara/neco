namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Neco.Common.Concurrency;

// TODO FileGetSTatistics and MostUsedMemoryCache(100Mib?)
public class CompressedStaticFilesMiddleware {
	private readonly RequestDelegate _next;
	private readonly ILoggerFactory _loggerFactory;
	private readonly ILogger<CompressedStaticFilesMiddleware> _logger;
	private readonly CompressedStaticFilesOptions _options;
	private readonly IContentTypeProvider _contentTypeProvider;
	private readonly IFileProvider _fileProvider;
	private readonly ConcurrentDictionary<String, IStaticFileInfo> _knownStaticFiles = new();
	private readonly IActionQueue _actionQueue;

	public CompressedStaticFilesMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<CompressedStaticFilesOptions> options, ILoggerFactory loggerFactory, IActionQueue? actionQueue) {
		_next = next;
		_loggerFactory = loggerFactory;
		_logger = loggerFactory.CreateLogger<CompressedStaticFilesMiddleware>();
		_options = options.Value ?? throw new ArgumentNullException(nameof(options));
		_contentTypeProvider = _options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
		_fileProvider = _options.FileProvider ?? (hostingEnv.WebRootFileProvider != null ? hostingEnv.WebRootFileProvider : throw new InvalidOperationException($"Missing {nameof(_options.FileProvider)}."));
		_actionQueue = actionQueue ?? new SimpleActionQueue(loggerFactory.CreateLogger<SimpleActionQueue>());
	}

	public Task InvokeAsync(HttpContext context) {
		if (_options.HonorEndpoints && context.GetEndpoint() != null) {
			_logger.LogTrace("Endpoint matched");
			return _next(context);
		}

		HttpRequest request = context.Request;

		// Validation
		if (!HttpMethods.IsHead(request.Method) && !HttpMethods.IsGet(request.Method)) {
			_logger.LogTrace("Request method not supported {Method}", request.Method);
			return _next(context);
		}

		if (!request.Path.StartsWithSegments(_options.RequestPath, out PathString remainingRequestPath)) {
			_logger.LogTrace("Request path {Path} does not match request filter", request.Path);
			return _next(context);
		}

		if (!TryLookupContentType(_contentTypeProvider, _options, remainingRequestPath.Value, out _)) {
			_logger.LogTrace("Request path {Path} does not match a supported file type", remainingRequestPath);
			return _next(context);
		}

		CompressionMethod clientRequestedCompression = CompressionMethod.None;
		if (context.Request.Headers.TryGetValue("Accept-Encoding", out StringValues acceptEncoding) && acceptEncoding.Count >= 1) {
			String ae = acceptEncoding[0];
			if (ae.AsSpan().IndexOf("br", StringComparison.Ordinal) >= 0) clientRequestedCompression = CompressionMethod.Brotli;
			else if (ae.AsSpan().IndexOf("gzip", StringComparison.Ordinal) >= 0) clientRequestedCompression = CompressionMethod.Gzip;
		}

		if (remainingRequestPath.Value.EndsWith(StaticFileInfo.CacheFileExtension, StringComparison.Ordinal)) {
			_logger.LogTrace("Will not serve cache file directly: {Path}", remainingRequestPath);
			return _next(context);
		}

		// TODO serve incompressible files and NONE directly

		if (!TryGetFileInfo(remainingRequestPath.Value, out IStaticFileInfo? fileInfo)) {
			_logger.LogTrace("File {Path} not found", remainingRequestPath);
			return _next(context);
		}

		if (clientRequestedCompression != CompressionMethod.None)
			fileInfo.EnsureCompression(_actionQueue, clientRequestedCompression);

		_logger.LogTrace("Serving {Path} from {FilePath}", remainingRequestPath, fileInfo);
		RequestHeaders requestHeaders = request.GetTypedHeaders();

		// 14.24 If-Match
		IList<EntityTagHeaderValue>? ifMatch = requestHeaders.IfMatch;
		if (ifMatch?.Count > 0) {
			for (Int32 index = 0; index < ifMatch.Count; index++) {
				EntityTagHeaderValue etag = ifMatch[index];
				if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(fileInfo.Etag, true)) {
					return fileInfo.SendFileResponse(context, clientRequestedCompression);
				}
			}

			return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status412PreconditionFailed, clientRequestedCompression);
		}

		// 14.26 If-None-Match
		IList<EntityTagHeaderValue>? ifNoneMatch = requestHeaders.IfNoneMatch;
		if (ifNoneMatch?.Count > 0) {
			for (Int32 index = 0; index < ifNoneMatch.Count; index++) {
				EntityTagHeaderValue etag = ifNoneMatch[index];
				if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(fileInfo.Etag, true)) {
					return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status304NotModified, clientRequestedCompression);
				}
			}

			return fileInfo.SendFileResponse(context, clientRequestedCompression);
		}

		DateTimeOffset now = DateTimeOffset.UtcNow;

		// 14.25 If-Modified-Since
		DateTimeOffset? ifModifiedSince = requestHeaders.IfModifiedSince;
		if (ifModifiedSince <= now) {
			if (ifModifiedSince < fileInfo.LastModified)
				return fileInfo.SendFileResponse(context, clientRequestedCompression);
			return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status304NotModified, clientRequestedCompression);
		}

		// 14.28 If-Unmodified-Since
		DateTimeOffset? ifUnmodifiedSince = requestHeaders.IfUnmodifiedSince;
		if (ifUnmodifiedSince <= now) {
			if (ifUnmodifiedSince >= fileInfo.LastModified)
				return fileInfo.SendFileResponse(context, clientRequestedCompression);
			return fileInfo.SendHeaderResponse(context.Response, StatusCodes.Status412PreconditionFailed, clientRequestedCompression);
		}

		// TODO Range from StaticFileContext

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

	private Boolean TryGetFileInfo(String path, [NotNullWhen(true)] out IStaticFileInfo? fileInfo) {
		if (TryGetFileInfoActual(path, out fileInfo)) return true;
		if (_options.ServeOnNotFound != null && TryGetFileInfoActual(_options.ServeOnNotFound, out fileInfo)) return true;
		return false;
	}
	private Boolean TryGetFileInfoActual(String path, [NotNullWhen(true)] out IStaticFileInfo? fileInfo) {
		if (_knownStaticFiles.TryGetValue(path, out fileInfo)) return true;

		IFileInfo? physicalFileInfo = _fileProvider.GetFileInfo(path);
		if (physicalFileInfo == null || !physicalFileInfo.Exists || String.IsNullOrEmpty(physicalFileInfo.PhysicalPath)) {
			return false;
		}

		_contentTypeProvider.TryGetContentType(physicalFileInfo.PhysicalPath, out String? contentType);

		StaticFileInfo sfi = new(physicalFileInfo, contentType, _loggerFactory.CreateLogger<StaticFileInfo>());
		if (!_knownStaticFiles.TryAdd(path, sfi)) return _knownStaticFiles.TryGetValue(path, out fileInfo);

		fileInfo = sfi;


		return true;
	}
}