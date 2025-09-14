namespace Neco.Common.Helper;

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class FileSystemHelper {
	/// <summary>
	/// Returns a unique filename dictated by the pattern. The parameter is an integer counted from 0
	/// </summary>
	/// <param name="filenamePattern">Must be a String.Format compatible pattern with only the {0} parameter set</param>
	/// <returns></returns>
	public static String GetUniqueFilenameFromPattern(String filenamePattern) {
		Int32 i = 0;
		String filename;

		do {
			filename = String.Format(filenamePattern, i++);
		} while (File.Exists(filename));

		return filename;
	}

	public static String GetUniqueFilename(String directory, String extension = "tmp") {
		String filename;
		do {
			filename = Path.Combine(directory, Path.ChangeExtension(Guid.NewGuid().ToString(), extension));
		} while (Directory.Exists(filename) || File.Exists(filename));


		return filename;
	}

	public static String GetUniquePathName(String directory) {
		String pathName;
		do {
			pathName = Path.Combine(directory, Path.GetRandomFileName());
		} while (Directory.Exists(pathName) || File.Exists(pathName));


		return pathName;
	}

	/// <summary>
	/// Checks two files for equality with byte-by-byte comparision
	/// </summary>
	/// <param name="f1">Filename of the first file</param>
	/// <param name="f2">Filename of the second file</param>
	/// <returns>TRUE if both files have the exact same contents; FALSE otherwise</returns>
	public static Boolean AreEqual(String f1, String f2) {
		if (String.IsNullOrWhiteSpace(f1) || String.IsNullOrWhiteSpace(f2) || !File.Exists(f1) || !File.Exists(f2))
			return false;

		if (Path.GetFullPath(f1) == Path.GetFullPath(f2))
			return true;

		const Int32 BufferSize = 10240;
		Byte[] buf1 = new Byte[BufferSize];
		Byte[] buf2 = new Byte[BufferSize];

		using (FileStream fs1 = new(f1, FileMode.Open)) {
			using FileStream fs2 = new(f2, FileMode.Open);
			if (fs1.Length != fs2.Length)
				return false;

			Int32 bytesRead1, bytesRead2;
			do {
				bytesRead1 = fs1.Read(buf1, 0, BufferSize);
				bytesRead2 = fs2.Read(buf2, 0, BufferSize);
				if (bytesRead1 != bytesRead2)
					return false;

				for (Int32 i = 0; i < bytesRead1; ++i)
					if (buf1[i] != buf2[i])
						return false;
			} while (bytesRead1 > 0 && bytesRead2 > 0);
		}

		return true;
	}

	public static Boolean CopyDirectory(String source, String target, Boolean recursive = true, ILogger? logger = null) {
		if (!Directory.Exists(source))
			return false;

		logger ??= NullLogger.Instance;

		if (!Directory.Exists(target)) {
			logger.LogDebug("Creating directory: {TargetDirectory}", target);
			Directory.CreateDirectory(target);
		}

		logger.LogDebug("Copying directory {SourceDirectory} to {TargetDirectory}", source, target);

		String[] filesInDirectory = Directory.GetFiles(source);
		foreach (String file in filesInDirectory) {
			String temppath = Path.Combine(target, Path.GetFileName(file));
			logger.LogTrace("Copying file {File} to {Temppath}", file, temppath);
			File.Copy(file, temppath);
		}

		if (!recursive) return true;

		// If copying subdirectories, copy them and their contents to new location.
		IEnumerable<String> directoriesInDirectory = Directory.EnumerateDirectories(source);
		foreach (String subdir in directoriesInDirectory) {
			String temppath = Path.Combine(target, Path.GetFileName(subdir));
			CopyDirectory(subdir, temppath, recursive, logger);
		}

		return true;
	}

	/// <summary>
	/// Reserved filenames in Ntfs
	/// <seealso cref="http://msdn.microsoft.com/en-us/library/aa365247%28VS.85%29.aspx"/>
	/// </summary>
	public static readonly String[] NtfsReservedFilenames = ["CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

	/// <summary>
	/// <para>Returns a safe filename</para>
	/// <para>This funcion may produce duplicate filenames</para>
	/// </summary>
	/// <param name="filename">The filename to sanitize, with or without extension, but NOT the path.</param>
	/// <returns>A safe to use filename</returns>
	public static String SanitizeFilename(String filename, String replacement = "-") {
		// replace invalid chars
		filename = Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c.ToString(), replacement, StringComparison.Ordinal));

		// defuse reserved filenames
		String filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);
		if (!String.IsNullOrEmpty(filenameWithoutExt)) {
			if (NtfsReservedFilenames.Contains(filenameWithoutExt.ToUpper()))
				filename = $"__{filenameWithoutExt}__{Path.GetExtension(filename)}";
		}

		// trim unwanted trailing chars
		return filename.TrimStart(' ').TrimEnd('.', ' ');
	}

	/// <summary>
	/// <para>Returns a safe filepath</para>
	/// <para>This funcion may produce duplicate filenames</para>
	/// <para>This will work with network shares '\\...' but not the '\\?\...' prefix</para>
	/// </summary>
	/// <param name="path">The path to sanitize</param>
	/// <returns>A safe to use filepath.</returns>
	public static String SanitizeFilepath(String path) {
		if (Path.IsPathRooted(path))
			path = Path.GetFullPath(path);

		String[] pathFragments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		for (Int32 i = 0; i < pathFragments.Length; ++i) {
			if (i == 0 && Path.IsPathRooted(path))
				continue;
			if (pathFragments[i] == "." || pathFragments[i] == "..")
				continue;

			pathFragments[i] = SanitizeFilename(pathFragments[i]);
		}

		return String.Join(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), pathFragments);
	}

	/// <summary>
	/// Check is one path is below another
	/// </summary>
	/// <param name="basePath">The base path</param>
	/// <param name="subPath">The path to be checked against basePath.</param>
	/// <param name="strictSubPath">false (default) to consider the same path to be a subPath and thus return true. </param>
	/// <returns>TRUE if subPath is a child path of basePath or both point to the same path, depending on strictness</returns>
	public static Boolean IsSameOrSubPath(String basePath, String subPath, Boolean strictSubPath = false) {
		String pathTrimmed = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		String subPathTrimmed = subPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		if (String.IsNullOrWhiteSpace(pathTrimmed) != String.IsNullOrWhiteSpace(subPathTrimmed)) return false;

		DirectoryInfo di1 = new(pathTrimmed);
		DirectoryInfo? di2 = new(subPathTrimmed);

		if (di1.FullName.Equals(di2.FullName, StringComparison.OrdinalIgnoreCase) && !strictSubPath)
			return true;

		Boolean isParent = false;
		while (di2.Parent != null) {
			if (di2.Parent.FullName == di1.FullName) {
				isParent = true;
				break;
			}

			di2 = di2.Parent;
		}

		return isParent;
	}
}