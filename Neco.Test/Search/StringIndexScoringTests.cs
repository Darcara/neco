namespace Neco.Test.Search;

using System;
using FluentAssertions;
using Neco.Search;
using NUnit.Framework;

[TestFixture]
public class StringIndexScoringTests {
	[Test]
	public void InDocumentScoreHighResolution() {
		StringIndex<Int32>.Score(9, 100, 0).Should().Be(0);
		StringIndex<Int32>.Score(9, 100, 1).Should().Be(1);
		StringIndex<Int32>.Score(9, 100, 12).Should().Be(1);
		StringIndex<Int32>.Score(9, 100, 13).Should().Be(2);
		StringIndex<Int32>.Score(9, 100, 24).Should().Be(2);
		StringIndex<Int32>.Score(9, 100, 25).Should().Be(3);
		StringIndex<Int32>.Score(9, 100, 37).Should().Be(3);
		StringIndex<Int32>.Score(9, 100, 38).Should().Be(4);
		StringIndex<Int32>.Score(9, 100, 49).Should().Be(4);
		StringIndex<Int32>.Score(9, 100, 50).Should().Be(5);
		StringIndex<Int32>.Score(9, 100, 62).Should().Be(5);
		StringIndex<Int32>.Score(9, 100, 63).Should().Be(6);
		StringIndex<Int32>.Score(9, 100, 74).Should().Be(6);
		StringIndex<Int32>.Score(9, 100, 75).Should().Be(7);
		StringIndex<Int32>.Score(9, 100, 87).Should().Be(7);
		StringIndex<Int32>.Score(9, 100, 88).Should().Be(8);
		StringIndex<Int32>.Score(9, 100, 99).Should().Be(8);
		
		// These are technically errors
		StringIndex<Int32>.Score(9, 100, 100).Should().Be(9);
		StringIndex<Int32>.Score(9, 100, 105).Should().Be(9);
		StringIndex<Int32>.Score(9, 100, 113).Should().Be(10);
	}

	[Test]
	public void InDocmentScoreLowResolution() {
		StringIndex<Int32>.Score(1, 100, 0).Should().Be(0);
		StringIndex<Int32>.Score(1, 100, 1).Should().Be(0);
		StringIndex<Int32>.Score(1, 100, 50).Should().Be(0);
		StringIndex<Int32>.Score(1, 100, 99).Should().Be(0);
	}
}