namespace Neco.Test.Common.Data.Archive;

using AwesomeAssertions;
using Neco.Common.Data.Archive;

[TestFixture]
public class FileEntryTests {
	[Test]
	public void ImplementsInterfaceCorrectly() {
		FileEntry fe = new("name", 123, 444, false, 0, 0);
		FileEntry feSame = new("name", 123, 444, false, 0, 0);
		FileEntry feOther = new("another", 123 + 444, 888, false, 0, 0);

		Assert.That(fe < feOther);
		Assert.That(fe <= feOther);
		Assert.That(feOther > fe);
		Assert.That(feOther >= fe);
		Assert.That(fe != feOther);
		Assert.That(fe == feSame);
		Assert.That(fe.Equals((Object?)fe));
		Assert.That(fe.Equals((Object?)feSame));
		Assert.That(fe.CompareTo((Object?)feSame), Is.Zero);
		Assert.That(fe.CompareTo((Object?)feOther), Is.Negative);
		Assert.That(feOther.CompareTo((Object?)feSame), Is.Positive);
		Assert.That(feOther.CompareTo((Object?)null), Is.Positive);
		Assert.That(feOther.GetHashCode(), Is.Not.Zero);
		fe.ToString().Should().Be(feSame.ToString());
	}
}