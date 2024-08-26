namespace Neco.Common.Data.Archive;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Neco.Common.Extensions;

public delegate Boolean IncludeFilePredicate(String relativeFilename, String nameInCatalog);

public interface IFileEnumerator : IEnumerable<EnumeratedFile> {
	/// <summary>
	/// Creates an exclusion filter from this enumerator
	/// </summary>
	/// <param name="enumerateOnce">
	/// TRUE (default) to enumerate this IFileEnumerator once and use an in-memory set for the queries. Requires more memory but is faster.
	/// FALSE will iterate this IFileEnumerator each time it is queries. Takes longer, but uses very little memory.
	/// </param>
	public IncludeFilePredicate Exclude(Boolean enumerateOnce = true) {
		if (enumerateOnce) {
			FrozenSet<String> set = this.Select(ef => ef.NameInCatalog).ToFrozenSet(StringComparer.Ordinal);
			return (_, nameInCatalog) => !set.Contains(nameInCatalog);
		}

		return (_, nameInCatalog) => -1 != this.FindIndex(itm => String.Equals(itm.NameInCatalog, nameInCatalog, StringComparison.Ordinal));
	}
}