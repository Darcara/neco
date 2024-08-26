namespace Neco.Common.Data.Archive;

using System;
using System.IO;

public static class FileEnumerators {
	public static IFileEnumerator Folder(String folder, String searchPattern, EnumerationOptions enumerationOptions, IncludeFilePredicate? includeFilePredicate = null, EntryNameGenerator? entryNameGenerator = null) {
		return new FolderEnumeration(folder, searchPattern, enumerationOptions, includeFilePredicate, entryNameGenerator);
	}

	public static IFileEnumerator Folder(String folder, String searchPattern, SearchOption enumerationOptions = SearchOption.AllDirectories, IncludeFilePredicate? includeFilePredicate = null, EntryNameGenerator? entryNameGenerator = null) {
		return new FolderSearch(folder, searchPattern, enumerationOptions, includeFilePredicate, entryNameGenerator);
	}

	public static IFileEnumerator Catalog(String catalog, IncludeFilePredicate? includeFilePredicate = null, Func<FileEntry, String>? entryNameGenerator = null) {
		return new CatalogContentEnumerator(catalog, includeFilePredicate, entryNameGenerator);
	}
}