namespace Neco.Common.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FileResolver {
	private readonly IEnumerable<String> _searchLocations;

	public static IEnumerable<String> WindowsFileSearchLocations(Boolean pathOnly = true) {
		String? pathEnv = Environment.GetEnvironmentVariable("PATH");
		if (!String.IsNullOrWhiteSpace(pathEnv)) {
			String[] pathSplit = pathEnv.Split(Path.PathSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			foreach (String pathEntry in pathSplit) {
				String expandedPathEntry = pathEntry;
				String oldPathEntry;
				do {
					oldPathEntry = expandedPathEntry;
					expandedPathEntry = Environment.ExpandEnvironmentVariables(expandedPathEntry);
				} while (!String.Equals(oldPathEntry, expandedPathEntry));

				yield return expandedPathEntry;
			}
		}

		if (pathOnly) yield break;
		
		yield return Environment.CurrentDirectory;
		yield return Environment.SystemDirectory;

		String? temp = Environment.GetEnvironmentVariable("TEMP");
		if (!String.IsNullOrWhiteSpace(temp))
			yield return temp;
		String? tmp = Environment.GetEnvironmentVariable("TMP");
		if (!String.IsNullOrWhiteSpace(tmp))
			yield return tmp;

		yield return Environment.GetFolderPath(Environment.SpecialFolder.System);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

		yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
		yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
	}

	public FileResolver(IEnumerable<String>? searchLocations) {
		_searchLocations = searchLocations ?? Array.Empty<String>();
	}

	public FileResolver(params String[] searchLocations) {
		_searchLocations = searchLocations;
	}

	public IEnumerable<FileInfo> Resolve(String filename, params String[] searchLocations) => Resolve(filename, (IEnumerable<String>)searchLocations);

	public IEnumerable<FileInfo> Resolve(String filename, IEnumerable<String>? searchLocations = null) {
		ArgumentNullException.ThrowIfNull(filename);
		
		foreach (String path in _searchLocations
			         .Concat(searchLocations ?? Array.Empty<String>())
			         .Where(sl => !String.IsNullOrWhiteSpace(sl))
			         .Select(Environment.ExpandEnvironmentVariables)) {

			FileInfo fi = new(Path.Combine(path, filename));
			if (fi.Exists) yield return fi;
		}
	}
}