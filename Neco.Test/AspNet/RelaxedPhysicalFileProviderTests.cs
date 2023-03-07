using NUnit.Framework;

namespace Neco.Test.AspNet;

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Neco.AspNet;

[TestFixture]
public class RelaxedPhysicalFileProviderTests {
	[Test]
	public void CanConstructWithNonExistingPath() {
		using RelaxedPhysicalFileProvider fileProvider = new(Path.GetFullPath("./IDoNotExist"));
		Assert.That(fileProvider, Is.Not.Null);
	}
	
	[Test]
	public void ReturnsNotFoundWithNonExistingPath() {
		using RelaxedPhysicalFileProvider fileProvider = new(Path.GetFullPath("./IDoNotExist"));
		IFileInfo fileInfo = fileProvider.GetFileInfo("./doesnotExist.txt");
		Assert.That(fileInfo, Is.Not.Null);
		Assert.That(fileInfo.Exists, Is.False);
		
		Assert.That(fileProvider.GetDirectoryContents("/"), Is.TypeOf<NotFoundDirectoryContents>());
	}
	
	[Test]
	public void ReturnsWithExistingPath() {
		using RelaxedPhysicalFileProvider fileProvider = new(Path.GetFullPath("./TestData"));
		Assert.That(fileProvider.Root, Is.EqualTo(Path.GetFullPath("./TestData/")));
		IFileInfo fileInfo = fileProvider.GetFileInfo("test.txt");
		Assert.That(fileInfo, Is.Not.Null);
		Assert.That(fileInfo.Exists, Is.True);
		Assert.That(fileInfo.Length, Is.EqualTo(448));

		IDirectoryContents directoryContents = fileProvider.GetDirectoryContents("/");
		Assert.That(directoryContents, Is.Not.Null);
		Assert.That(directoryContents.Exists, Is.True);
		Assert.That(directoryContents.Count(), Is.GreaterThanOrEqualTo(1));

		Assert.That(fileProvider.GetFileInfo("/.dotTestFile"), Is.TypeOf<NotFoundFileInfo>());
		
		Assert.That(fileProvider.GetDirectoryContents("c:/rooted/Path"), Is.TypeOf<NotFoundDirectoryContents>());
		Assert.That(fileProvider.GetDirectoryContents("\\rooted\\Path"), Is.TypeOf<NotFoundDirectoryContents>());
		Assert.That(fileProvider.GetDirectoryContents("::invalid::"), Is.TypeOf<NotFoundDirectoryContents>());
		Assert.That(fileProvider.GetDirectoryContents("../../system32"), Is.TypeOf<NotFoundDirectoryContents>());
		Assert.That(fileProvider.GetFileInfo("c:/rooted/Path"), Is.TypeOf<NotFoundFileInfo>());
		Assert.That(fileProvider.GetFileInfo("c:\\rooted\\Path"), Is.TypeOf<NotFoundFileInfo>());
		Assert.That(fileProvider.GetFileInfo("\\rooted\\Path.txt").Exists, Is.False);
		Assert.That(fileProvider.GetFileInfo("::invalid::"), Is.TypeOf<NotFoundFileInfo>());
		Assert.That(fileProvider.GetFileInfo("../../system32"), Is.TypeOf<NotFoundFileInfo>());
	}
	
	[Test]
	public void CanCreateFileWatcher() {
		using RelaxedPhysicalFileProvider fileProvider = new(Path.GetFullPath("./TestData"));
		Assert.That(fileProvider.UsePollingFileWatcher, Is.False);
		Assert.That(fileProvider.UseActivePolling, Is.False);
		
		IChangeToken changeToken = fileProvider.Watch("c:/windows");
		Assert.That(changeToken, Is.Not.Null);
		Assert.That(changeToken, Is.TypeOf<NullChangeToken>());
		
		changeToken = fileProvider.Watch("*");
		Assert.That(changeToken, Is.Not.Null);
		Assert.That(changeToken.HasChanged, Is.False);
		using FileStream? fs = File.Create("./TestData/deleteme", 1 , FileOptions.DeleteOnClose);
		fs.WriteByte(1);
		fs.Flush();
		Assert.That(() => changeToken.HasChanged, Is.True.After(100, 15));
	}
	
	[Test]
	public void FailsToCreateFileWatcherWithNonExistingPath() {
		using RelaxedPhysicalFileProvider fileProvider = new(Path.GetFullPath("./IDoNotExist"));
		Assert.Throws<ArgumentException>(() => fileProvider.Watch("*"));
	}
	
	[Test]
	public void ThrowsWithNonRootedPath() {
		Assert.Throws<ArgumentException>(() => new RelaxedPhysicalFileProvider("./TestData"));
	}

}