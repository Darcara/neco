namespace Neco.Test.Common.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Neco.Common.Data;
using NUnit.Framework;

[TestFixture]
public class FileResolverTests {
	[Test]
	public void EnumeratesSearchDirectories() {
		List<String> directories = FileResolver.WindowsFileSearchLocations(false).ToList();
		Console.WriteLine($"Search directories:{Environment.NewLine}{String.Join(Environment.NewLine, directories)}");
		directories.Should().Contain(Environment.CurrentDirectory);
	}
	
	[Test]
	public void CanResolve() {
		String filename = typeof(FileResolverTests).Assembly.GetName().Name + ".dll";
		List<FileInfo> fileInfos = new FileResolver(FileResolver.WindowsFileSearchLocations(false)).Resolve(filename).ToList();
		fileInfos.Should().HaveCount(1);
	}
}