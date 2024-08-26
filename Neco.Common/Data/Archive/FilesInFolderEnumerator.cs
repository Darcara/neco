namespace Neco.Common.Data.Archive;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public delegate String EntryNameGenerator(DirectoryInfo baseDirectory, FileInfo file);

public abstract class FilesInFolderEnumerator : IFileEnumerator {
	private static readonly EntryNameGenerator _defaultRelativeNameGenerator = (directory, file) => {
		String name = Path.GetRelativePath(directory.FullName, file.FullName);
		return Path.IsPathRooted(name) ? file.Name : name;
	};

	internal static readonly IncludeFilePredicate DefaulIncludeAllPredicate = (_, _) => true;

	protected readonly EntryNameGenerator EntryNameGenerator;
	protected readonly IncludeFilePredicate IncludeFilePredicate;
	public DirectoryInfo Folder { get; }
	public String SearchPattern { get; }

	public FilesInFolderEnumerator(String folder, String searchPattern, IncludeFilePredicate? includeFilePredicate = null, EntryNameGenerator? entryNameGenerator = null) {
		ArgumentException.ThrowIfNullOrEmpty(folder);
		ArgumentException.ThrowIfNullOrEmpty(searchPattern);
		EntryNameGenerator = entryNameGenerator ?? _defaultRelativeNameGenerator;
		IncludeFilePredicate = includeFilePredicate ?? DefaulIncludeAllPredicate;
		Folder = new DirectoryInfo(folder);
		if (!Folder.Exists) throw new ArgumentException($"Folder {folder} does not exist", nameof(folder));
		SearchPattern = searchPattern;
	}

	#region Implementation of IEnumerable<out FileInfo>

	/// <inheritdoc />
	public abstract IEnumerator<EnumeratedFile> GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion
}