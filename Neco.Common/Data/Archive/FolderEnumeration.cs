namespace Neco.Common.Data.Archive;

using System;
using System.Collections.Generic;
using System.IO;

public class FolderEnumeration : FilesInFolderEnumerator {
	public EnumerationOptions EnumerationOptions { get; }

	/// <inheritdoc />
	public FolderEnumeration(String folder, String searchPattern, EnumerationOptions enumerationOptions, IncludeFilePredicate? includeFilePredicate = null, EntryNameGenerator? entryNameGenerator = null) : base(folder, searchPattern, includeFilePredicate, entryNameGenerator) {
		EnumerationOptions = enumerationOptions;
	}

	#region Overrides of FilesInFolderEnumerator

	/// <inheritdoc />
	public override IEnumerator<EnumeratedFile> GetEnumerator() {
		foreach (FileInfo enumerateFile in Folder.EnumerateFiles(SearchPattern, EnumerationOptions)) {
			String nameInCatalog = EntryNameGenerator(Folder, enumerateFile);
			if (IncludeFilePredicate(Path.GetRelativePath(Folder.FullName, enumerateFile.FullName), nameInCatalog))
				yield return new EnumeratedFile(nameInCatalog, () => enumerateFile.OpenRead());
		}
	}

	#endregion
}