namespace Neco.Test.Search;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Neco.Common.Data.Hash;
using Neco.Search;
using NUnit.Framework;

[TestFixture]
public class DataTests {
	[Test]
	public void AllWordsWyHashToDifferentValues() {
		HashSet<String> words = new(StringComparer.Ordinal);
		SimpleWhitespaceSplitter splitter = new();
		foreach (String document in Data.AvailableDocuments.Select(name => Helper.ReadCompressedFileAsString(name))) {
			String uppercaseDocument = document.ToUpperInvariant();
			words.UnionWith(splitter.Split(uppercaseDocument));
			
			String doc = uppercaseDocument.Replace("-", String.Empty);
			words.UnionWith(splitter.Split(doc));
			
			doc = uppercaseDocument.Replace("-", " ");
			words.UnionWith(splitter.Split(doc));
		}

		words.Add("");
		words.Add(" ");
		words.Add("  ");
		words.Add("-");
		
		Console.WriteLine($"Checking {words.Count} distinct words");
		Dictionary<UInt64, List<String>> hashes = new();
		Int64 numCollisions = 0;
		foreach (String word in words) {
			var hash = WyHashFinal3.HashOneOffLong(Encoding.UTF8.GetBytes(word));
			if (!hashes.TryAdd(hash, new(1){word})) {
				var collisionList = hashes[hash];
				collisionList.Add(word);
				Console.WriteLine($"There are {collisionList.Count} collisions for hash {hash:X8}: " + String.Join(", ", collisionList));
				++numCollisions;
			}
		}
		
		Assert.That(numCollisions, Is.Zero);
	}
}