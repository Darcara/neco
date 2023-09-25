namespace Neco.Test.Common.Extensions;

using System;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class RandomExtensionTests {
	[Test]
	public void BasicTests() {
		Assert.That(Random.Shared.NextUInt32(), Is.Not.Negative);
		Assert.That(Random.Shared.NextUInt64(), Is.Not.Negative);
	}
}