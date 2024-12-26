namespace Neco.Test.Search;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnyAscii;
using DawgSharp;
using FluentAssertions;
using Neco.Common.Extensions;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public partial class DawgTests {
	[GeneratedRegex(@"[""'\[\]\(\):\.!\?,;_\s\t\r\n\f\u0085\u2028\u2029]+")]
	private static partial Regex WordSplitRegex();

	[GeneratedRegex("[A-Z]")]
	private static partial Regex WordCharacterRegex();

	[Test]
	public void DawgSimpleSearch() {
		List<String> documents = Data.AvailableDocuments.ToList();
		MultiDawg<Int32> dawg = BuildDawg();
		
		WriteMatches("BLOOD");
		WriteMatches("TYBA");
		WriteMatches("ANON");
		WriteMatches("FISH");

		return;

		void WriteMatches(String s) {
			List<KeyValuePair<String, IEnumerable<Int32>>> keyValuePairs = dawg.MatchPrefix(s).ToList();
			keyValuePairs.Count.Should().BePositive();
			Console.WriteLine($"Search for '{s}' yielded {keyValuePairs.Count} results");
			keyValuePairs
				.Select(match => $"  {match.Key}({match.Value.Count()} of {Data.NumberOfAvailableDocuments}) = {String.Join(", ", match.Value.Select(v => documents[v]))}")
				.ForEach(Console.WriteLine);
		}
	}

	private MultiDawg<Int32> BuildDawg() {
		ConstructDawg("testDawg.bin");

		GC.Collect(2, GCCollectionMode.Aggressive, true, true);
		Int64 allocatedBefore = GC.GetTotalMemory(true);
		Stopwatch sw = Stopwatch.StartNew();

		MultiDawg<Int32> dawg = LoadDawg("testDawg.bin");

		sw.Stop();
		GC.Collect(2, GCCollectionMode.Aggressive, true, true);
		Int64 allocatedAfter = GC.GetTotalMemory(true);
		Console.WriteLine($"DAWG loaded from disk in {sw.Elapsed} needs {(allocatedAfter - allocatedBefore).ToFileSize()} = {allocatedBefore.ToFileSize()} -> {allocatedAfter.ToFileSize()}");


		return dawg;
	}

	private static MultiDawg<Int32> LoadDawg(String filename) {
		MultiDawg<Int32> dawg;
		using FileStream fs = File.OpenRead(filename);
		dawg = MultiDawgBuilder<Int32>.LoadFrom(fs);

		return dawg;
	}

	private void ConstructDawg(String filename) {
		GC.Collect(2, GCCollectionMode.Aggressive, true, true);
		Int64 allocatedBefore = GC.GetTotalMemory(true);
		Stopwatch sw = Stopwatch.StartNew();

		Int64 wordCount = 0;

		MultiDawgBuilder<Int32> dawgBuilder = new();
		List<String> documents = new();
		HashSet<String> allWords = new(StringComparer.Ordinal);

		foreach (String documentName in Data.AvailableDocuments) {
			documents.Add(documentName);
			String document = Helper.ReadCompressedFileAsString(documentName).Transliterate().ToUpperInvariant();
			HashSet<String> words = WordSplitRegex().Split(document).Select(w => w.Replace("-", String.Empty)).ToHashSet(StringComparer.Ordinal);
			allWords.UnionWith(words);

			wordCount += words.Count;
			AddWordsToDawg(words.ToArray(), dawgBuilder, documents.Count - 1, 3);
		}

		Console.WriteLine($"Found {allWords.Count} distinct words in {documents.Count} documents");
		Console.WriteLine("Invalid chars: " + String.Join(", ", allWords.SelectMany(w => WordCharacterRegex().Replace(w, String.Empty)).ToHashSet()));
		Console.WriteLine("Longest words:");
		allWords.OrderByDescending(w => w.Length).Take(10).ForEach(w => Console.WriteLine($"  {w.Length} = {w[..Math.Min(50, w.Length)]}"));
		allWords.Clear();
		documents.Clear();

		MultiDawg<Int32> dawg = dawgBuilder.BuildMultiDawg();
		FileInfo persistedIndex = new(filename);
		using (FileStream fs = persistedIndex.Create())
			dawgBuilder.SaveTo(fs);
		persistedIndex.Refresh();

		sw.Stop();
		GC.Collect(2, GCCollectionMode.Aggressive, true, true);
		Int64 allocatedAfter = GC.GetTotalMemory(true);
		Console.WriteLine($"Text with {wordCount} words indexed in {sw.Elapsed}");
		Console.WriteLine($"DAWG with {dawg.GetNodeCount()} nodes and {dawg.MaxPayloads} payloads needs {(allocatedAfter - allocatedBefore).ToFileSize()} = {allocatedBefore.ToFileSize()} -> {allocatedAfter.ToFileSize()} -- Filesize is {persistedIndex.Length.ToFileSize()}");
	}

	private static void AddWordsToDawg<T>(String[] words, MultiDawgBuilder<T> dawgBuilder, T value, Int32 prefixSize = 2) {
		foreach (String word in words) {
			if (dawgBuilder.TryGetValue(word, out IList<T>? currentValues)) {
				if (!currentValues.Contains(value)) currentValues.Add(value);
			} else {
				dawgBuilder.Insert(word, [value]);
			}

			for (Int32 i = 1; i < word.Length - prefixSize; i++) {
				if (dawgBuilder.TryGetValue(word.Skip(i), out IList<T>? currentValues2)) {
					if (!currentValues2.Contains(value)) currentValues2.Add(value);
				} else {
					dawgBuilder.Insert(word.Skip(i), [value]);
				}
			}
		}
	}

	/*
		PREFIX-3-SUCCESS 3,578,413 ops in 5,000.001ms = clean per operation: 1.367µs or 731,768.630op/s with GC 1242/3/0
		PREFIX-3-SUCCESS TotalCPUTime per operation: 5,015.625ms or clean 729,438.066op/s for a factor of 1.003
		PREFIX-5-SUCCESS 15,870,922 ops in 5,000.001ms = clean per operation: 0.284µs or 3,515,571.999op/s with GC 1351/2/0
		PREFIX-5-SUCCESS TotalCPUTime per operation: 5,031.250ms or clean 3,491,404.510op/s for a factor of 1.006
		SUFFIX-3-SUCCESS 533,655 ops in 5,000.001ms = clean per operation: 9.339µs or 107,080.412op/s with GC 1310/3/0
		SUFFIX-3-SUCCESS TotalCPUTime per operation: 5,000.000ms or clean 107,080.433op/s for a factor of 1.000
		SUFFIX-5-SUCCESS 17,604,625 ops in 5,000.001ms = clean per operation: 0.253µs or 3,946,621.926op/s with GC 1313/2/0
		SUFFIX-5-SUCCESS TotalCPUTime per operation: 5,000.000ms or clean 3,946,622.811op/s for a factor of 1.000
		3-CHAR-FAIL 28,916,752 ops in 5,000.001ms = clean per operation: 0.142µs or 7,029,139.240op/s with GC 1244/0/0
		3-CHAR-FAIL TotalCPUTime per operation: 5,000.000ms or clean 7,029,140.949op/s for a factor of 1.000
		5-CHAR-FAIL 25,910,983 ops in 5,000.001ms = clean per operation: 0.162µs or 6,167,060.828op/s with GC 1115/0/0
		5-CHAR-FAIL TotalCPUTime per operation: 5,015.625ms or clean 6,144,212.440op/s for a factor of 1.003
	 */
	[Test]
	public void DawgRoughPerformance() {
		MultiDawg<Int32> dawg = BuildDawg();

		Int32 count = dawg.MatchPrefix("TYBALT").Count();
		Console.WriteLine($"FOUND TYBALT: {count}");

		count = dawg.MatchPrefix("TYBA").Count();
		Console.WriteLine($"FOUND TYBA__: {count}");

		count = dawg.MatchPrefix("YBALT").Count();
		Console.WriteLine($"FOUND _YBALT: {count}");

		count = dawg.MatchPrefix("YBAL").Count();
		Console.WriteLine($"FOUND _YBAL_: {count}");

		Console.WriteLine("Performance test cases:");
		Console.WriteLine($"PREFIX-3 TYB   : {dawg.MatchPrefix("TYB").Count()}");
		Console.WriteLine($"PREFIX-5 TYBAL : {dawg.MatchPrefix("TYBAL").Count()}");
		Console.WriteLine($"SUFFIX-3    ALT: {dawg.MatchPrefix("ALT").Count()}");
		Console.WriteLine($"SUFFIX-5  YBALT: {dawg.MatchPrefix("YBALT").Count()}");

		Assert.That(dawg.MatchPrefix("UWU").Count(), Is.Zero);
		Assert.That(dawg.MatchPrefix("OWO").Count(), Is.Positive);
		Assert.That(dawg.MatchPrefix("OWOWO").Count(), Is.Zero);

		PerformanceHelper.GetPerformanceRough("PREFIX-3-SUCCESS", dwg => {
			Int32 i = dwg.MatchPrefix("TYB").Count();
		}, dawg);
		PerformanceHelper.GetPerformanceRough("PREFIX-5-SUCCESS", dwg => {
			Int32 i = dwg.MatchPrefix("TYBAL").Count();
		}, dawg);
		PerformanceHelper.GetPerformanceRough("SUFFIX-3-SUCCESS", dwg => {
			Int32 i = dwg.MatchPrefix("ALT").Count();
		}, dawg);
		PerformanceHelper.GetPerformanceRough("SUFFIX-5-SUCCESS", dwg => {
			Int32 i = dwg.MatchPrefix("YBALT").Count();
		}, dawg);

		PerformanceHelper.GetPerformanceRough("3-CHAR-FAIL", dwg => {
			Int32 i = dwg.MatchPrefix("UWU").Count();
		}, dawg);
		PerformanceHelper.GetPerformanceRough("5-CHAR-FAIL", dwg => {
			Int32 i = dwg.MatchPrefix("OWOWO").Count();
		}, dawg);
	}
}