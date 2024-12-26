namespace Neco.Test.Search;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class Data {
	public const String Folder = "TestData";

	public static Int32 NumberOfAvailableDocuments { get; } = AvailableDocuments.Count();

	public static IEnumerable<String> AvailableDocuments {
		get {
			yield return "GulliversTravel.txt";
			yield return "MobyDick.txt";

			foreach (FileInfo? file in new DirectoryInfo(Path.Combine(Folder, "shakespeare")).EnumerateFiles()) {
				yield return Path.GetRelativePath(Path.GetFullPath(Folder), file.FullName);
			}
		}
	}
	// https://www.gutenberg.org/files/1661/1661-0.txt
	// Most common names in Sherlok Holmes: "Holmes|Watson|Lestrade|Hudson|Moriarty|Adler|Moran|Morstan|Gregson"
}