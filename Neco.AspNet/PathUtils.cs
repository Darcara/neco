// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Neco.AspNet;

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;

internal static class PathUtils {
	private static readonly Char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
		.Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar).ToArray();

	private static readonly Char[] _invalidFilterChars = _invalidFileNameChars
		.Where(c => c != '*' && c != '|' && c != '?').ToArray();

	private static readonly Char[] _pathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

	internal static Boolean HasInvalidPathChars(String path) => path.IndexOfAny(_invalidFileNameChars) != -1;

	internal static Boolean HasInvalidFilterChars(String path) => path.IndexOfAny(_invalidFilterChars) != -1;

	internal static String EnsureTrailingSlash(String path) {
		if (!String.IsNullOrEmpty(path) &&
		    path[^1] != Path.DirectorySeparatorChar) {
			return path + Path.DirectorySeparatorChar;
		}

		return path;
	}

	internal static Boolean PathNavigatesAboveRoot(String path) {
		StringTokenizer tokenizer = new(path, _pathSeparators);
		Int32 depth = 0;

		foreach (StringSegment segment in tokenizer) {
			if (segment.Equals(".") || segment.Equals("")) {
				continue;
			}

			if (segment.Equals("..")) {
				depth--;

				if (depth == -1) {
					return true;
				}
			} else {
				depth++;
			}
		}

		return false;
	}
}