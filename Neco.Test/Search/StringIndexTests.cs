namespace Neco.Test.Search;

using System;
using System.Linq;
using Neco.Search;
using NUnit.Framework;

[TestFixture]
public class StringIndexTests {

	[Test]
	public void CanRead() {
		String content = Helper.ReadCompressedFileAsString(Data.AvailableDocuments.First());
		Assert.That(content, Is.Not.Empty);

		StringIndex<Guid> index = new();
		Guid documentId = Guid.NewGuid();
		index.Add(documentId, content);
		
	}
}