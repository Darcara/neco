namespace Neco.Test.Common.Extensions;

using System.Text;
using FluentAssertions;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class StringBuilderExtensionTests {
	[Test]
	public void AppendLower() {
		StringBuilder sb = new();
		sb.AppendLowerInvariant("Asdf");
		sb.ToString().Should().Be("asdf");
		
		sb.AppendLowerInvariant(null);
		sb.ToString().Should().Be("asdf");
	}
	
	[Test]
	public void AppendUpper() {
		StringBuilder sb = new();
		sb.AppendUpperInvariant("Asdf");
		sb.ToString().Should().Be("ASDF");

		sb.AppendUpperInvariant(null);
		sb.ToString().Should().Be("ASDF");
	}
}