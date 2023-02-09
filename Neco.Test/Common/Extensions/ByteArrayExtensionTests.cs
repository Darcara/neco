using NUnit.Framework;

namespace Neco.Test.Common.Extensions;

using System;
using Neco.Common.Extensions;

[TestFixture]
public class ByteArrayExtensionTests {
	[Test]
	public void StringHexSingleLine() {
		Byte[] array = {0x34, 0xFF, 0x00, 0xAB, 0xC5};
		Assert.That(array.ToStringHexSingleLine(), Is.EqualTo("34FF00ABC5"));
		Assert.That(array.ToStringHexSingleLine(2), Is.EqualTo("00ABC5"));
		Assert.That(array.ToStringHexSingleLine(2, 2), Is.EqualTo("00AB"));
	}
}