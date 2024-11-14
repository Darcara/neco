namespace Neco.Common.Extensions;

using System;
using System.Collections.Generic;
using Neco.Common.Helper;

public static class UriExtensions {
	/// <summary>
	/// Returns an array similar to <see cref="Uri.Segments"/>, but without the forward slashes. Also works for relative Uris.
	/// </summary>
	public static String[] SegmentsWithoutDelimiter(this Uri uri) {
		if (!uri.IsAbsoluteUri)
			uri = new(UriHelper.ExampleBaseUri, uri);
		String path = uri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped);

		if (path.Length == 0)
			return [];

		List<String> segments = new(5);
		Int32 index = 0;
		while (index < path.Length) {
			Int32 next = path.IndexOf('/', index);
			if (next == -1) {
				String segment = path.Substring(index, path.Length - index);
				segments.Add(segment);
				break;
			}

			if (next - index > 0) {
				// It's not just the '/'
				String segment = path.Substring(index, next - index);
				segments.Add(segment);
			}

			index = next + 1;
		}

		return segments.ToArray();
	}
}