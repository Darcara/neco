// ReSharper disable once CheckNamespace

namespace Microsoft.AspNetCore.Builder;

using System;

[Flags]
public enum SingleFileServeOptions {
	None = 0,
	Compress = 1,
	Cachable = 2,
	Uncachable = 4,
}