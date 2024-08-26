namespace Neco.Common.Data.Archive;

using System;
using System.Collections.Generic;
using System.IO;

public class FolderSearch : FilesInFolderEnumerator {
	public SearchOption SearchOptions { get; }

	/// <inheritdoc />
	public FolderSearch(String folder, String searchPattern, SearchOption searchOptions, IncludeFilePredicate? includeFilePredicate = null, EntryNameGenerator? entryNameGenerator = null) : base(folder, searchPattern, includeFilePredicate, entryNameGenerator) {
		SearchOptions = searchOptions;
	}

	#region Overrides of FilesInFolderEnumerator

	/// <inheritdoc />
	public override IEnumerator<EnumeratedFile> GetEnumerator() {
		foreach (FileInfo enumerateFile in Folder.EnumerateFiles(SearchPattern, SearchOptions)) {
			String nameInCatalog = EntryNameGenerator(Folder, enumerateFile);
			if (IncludeFilePredicate(Path.GetRelativePath(Folder.FullName, enumerateFile.FullName), nameInCatalog))
				yield return new EnumeratedFile(nameInCatalog, () => enumerateFile.OpenRead());
		}
	}

	#endregion
}