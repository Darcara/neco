namespace Neco.Test.Search;

using System;
using System.Linq;
using Neco.Common.Extensions;
using Neco.Search;
using NUnit.Framework;

[TestFixture]
public class TokenizerTests {
	[Test]
	public void WhitespaceTokenizer() {
		String content = Helper.ReadCompressedFileAsString(Data.AvailableDocuments.First());
		String[] words = new SimpleWhitespaceSplitter().Split(content);
		var wsIndex = words.FindIndex(String.IsNullOrWhiteSpace);
		Assert.That(wsIndex, Is.Negative, () => String.Join("###", words.Skip(wsIndex-5). Take(10)));
		Assert.That(words.All(t => !String.IsNullOrWhiteSpace(t)), Is.True);
	}
}