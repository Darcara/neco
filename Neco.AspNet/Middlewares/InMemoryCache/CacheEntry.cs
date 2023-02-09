namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

public sealed class CacheEntry {
	public DateTime Created { get; }
	public Int32 StatusCode { get; }
	public IHeaderDictionary OriginalHeaders { get; }
	public EntityTagHeaderValue? Etag { get; }
	public DateTimeOffset? LastModified { get; }

	public Int64 BodyLength { get; }
	public List<Byte[]>? Data { get; }

	public CacheEntry(DateTime created, Int32 statusCode, IHeaderDictionary originalHeaders, EntityTagHeaderValue? etag, DateTimeOffset? lastModified, Int64 bodyLength, List<Byte[]>? data) {
		Created = created;
		StatusCode = statusCode;
		OriginalHeaders = originalHeaders;
		Etag = etag;
		LastModified = lastModified;
		BodyLength = bodyLength;
		Data = data;
	}

	public async Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken) {
		if (Data == null) return;
		foreach (Byte[] bytes in Data) {
			cancellationToken.ThrowIfCancellationRequested();
			// Faster than WriteAsync apparently
			Copy(destination, bytes);
			await destination.FlushAsync(cancellationToken);
		}
	}

	private static void Copy(PipeWriter destination, Byte[] bytes) {
		Span<Byte> span = destination.GetSpan(bytes.Length);
		bytes.CopyTo(span);
		destination.Advance(bytes.Length);
	}
}