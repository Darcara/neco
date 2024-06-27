namespace Neco.Test.Common.Extensions;

using System;
using System.Collections.Generic;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class UriExtensionTests {
	private static IEnumerable<TestCaseData> _testCases {
		get {
			yield return new TestCaseData("", (String[])[]);
			yield return new TestCaseData("/", (String[])[]);
			
			yield return new TestCaseData("folder1", new[] {"folder1"});
			yield return new TestCaseData("folder1/", new[] {"folder1"});
			yield return new TestCaseData("/folder1", new[] {"folder1"});
			yield return new TestCaseData("/folder1/", new[] {"folder1"});
			yield return new TestCaseData("/folder1/?query", new[] {"folder1"});
			yield return new TestCaseData("/folder1?query", new[] {"folder1"});
			
			yield return new TestCaseData("folder1/folder2", new[] {"folder1", "folder2"});
			yield return new TestCaseData("folder1/folder2/", new[] {"folder1", "folder2"});
			yield return new TestCaseData("/folder1/folder2", new[] {"folder1", "folder2"});
			yield return new TestCaseData("/folder1/folder2/", new[] {"folder1", "folder2"});
			yield return new TestCaseData("/folder1/folder2/?query", new[] {"folder1", "folder2"});
			yield return new TestCaseData("/folder1/folder2?query", new[] {"folder1", "folder2"});
		}
	}

	[TestCaseSource(nameof(_testCases))]
	public void SegmentsAbsoluteUrisWithoutDelimiterCorrectly(String uri, String[] expectedSegments) {
		Uri testMe = new(new Uri("https://example.org"), uri);
		String[] segments = testMe.SegmentsWithoutDelimiter();
		Assert.That(segments, Is.EqualTo(expectedSegments).AsCollection);
	}
	
	[TestCaseSource(nameof(_testCases))]
	public void SegmentsRelativeUrisWithoutDelimiterCorrectly(String uri, String[] expectedSegments) {
		Uri testMe = new(uri, UriKind.Relative);
		String[] segments = testMe.SegmentsWithoutDelimiter();
		Assert.That(segments, Is.EqualTo(expectedSegments).AsCollection);
	}
}