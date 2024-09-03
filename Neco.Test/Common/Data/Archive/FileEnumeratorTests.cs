namespace Neco.Test.Common.Data.Archive;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Neco.Common.Data.Archive;
using NUnit.Framework;

[TestFixture]
public class FileEnumeratorTests {
	[Test]
	public void FolderSearchTests() {
		List<EnumeratedFile> testDataFiles = FileEnumerators.Folder("./TestData", "*.*").ToList();
		testDataFiles.Any(file => file.NameInCatalog == "test.txt").Should().BeTrue();
		testDataFiles.Count.Should().BeGreaterThan(3);

		EnumeratedFile comparisonFile = new("test.txt", null!);
		testDataFiles.Single(file => file == comparisonFile).Should().NotBeNull();
		testDataFiles.Count(file => file != comparisonFile).Should().Be(testDataFiles.Count - 1);

		comparisonFile.GetHashCode().Should().NotBe(0);
		comparisonFile.ToString().Should().NotBeNull();
		comparisonFile.Equals(comparisonFile).Should().BeTrue();
		comparisonFile.Equals(null).Should().BeFalse();
	}

	[Test]
	public void FolderEnumerationTests() {
		List<EnumeratedFile> testDataFiles = FileEnumerators.Folder("./TestData", "*.*", new EnumerationOptions() { RecurseSubdirectories = false }).ToList();
		testDataFiles.Any(file => file.NameInCatalog == "test.txt").Should().BeTrue();
		testDataFiles.Count.Should().BeGreaterThanOrEqualTo(3);
	}

	[Test]
	public void CatalogEnumerationTests() {
		CatalogTests.BeforeEachTest();
		CatalogTests.CreateTestCatalog();

		List<EnumeratedFile> testDataFiles = FileEnumerators.Catalog(CatalogTests.TestCatalogName).ToList();
		testDataFiles.Count.Should().Be(2);
		testDataFiles.FindIndex(file => file.NameInCatalog.StartsWith(typeof(FileEntryTests).Assembly.GetName().Name ?? "")).Should().NotBe(-1);
	}

	[Test]
	public void EnumerationExclusionTests() {
		CatalogTests.BeforeEachTest();
		CatalogTests.CreateTestCatalog();

		IFileEnumerator catalogEnumerator = FileEnumerators.Catalog(CatalogTests.TestCatalogName);
		String name = Path.GetFileName(typeof(FileEntryTests).Assembly.Location);
		catalogEnumerator.Any(ef => String.Equals(ef.NameInCatalog, name, StringComparison.Ordinal)).Should().BeTrue();
		
		List<EnumeratedFile> testDataFiles = FileEnumerators.Folder(".", "*.*", SearchOption.TopDirectoryOnly, catalogEnumerator.Exclude()).ToList();
		testDataFiles.Any(ef => String.Equals(ef.NameInCatalog, name, StringComparison.Ordinal)).Should().BeFalse();
		testDataFiles.Count.Should().BeGreaterThan(2);
	}

}