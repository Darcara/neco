namespace Neco.Common.Data.Archive;

using System;
using System.Collections;
using System.Collections.Generic;

public sealed class CatalogContentEnumerator : IFileEnumerator {
	internal static readonly Func<FileEntry, String> DefaulIdentyNameGenerator = fe => fe.Name;

	private readonly IncludeFilePredicate _includeFilePredicate;
	private readonly Func<FileEntry, String> _entryNameGenerator;
	public String CatalogName { get; }

	public CatalogContentEnumerator(String catalogName, IncludeFilePredicate? includeFilePredicate, Func<FileEntry, String>? entryNameGenerator) {
		CatalogName = catalogName;
		_includeFilePredicate = includeFilePredicate ?? FilesInFolderEnumerator.DefaulIncludeAllPredicate;
		_entryNameGenerator = entryNameGenerator ?? DefaulIdentyNameGenerator;
	}

	#region Implementation of IEnumerable

	/// <inheritdoc />
	public IEnumerator<EnumeratedFile> GetEnumerator() {
		using Catalog catalog = Catalog.OpenExisting(CatalogName);
		for (Int32 i = 0; i < catalog.Entries.Count; i++) {
			FileEntry catalogEntry = catalog.Entries[i];
			String nameInCatalog = _entryNameGenerator(catalogEntry);
			if (!_includeFilePredicate(catalogEntry.Name, nameInCatalog)) continue;
			// ReSharper disable once AccessToDisposedClosure -- This is possible even on a disposed catalog
			yield return new EnumeratedFile(nameInCatalog, () => catalog.GetDataAsStandaloneStream(catalogEntry));
		}
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion
}