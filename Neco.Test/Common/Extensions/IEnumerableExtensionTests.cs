namespace Neco.Test.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class IEnumerableExtensionTests {
	private String[] _strings = { "55555", "333", "7777777", "666666" };

	[Test]
	public void MinMax() {
		Int32 count = Enumerable.Range(4, 14).MinMax(out Int32 min, out Int32 max);
		Assert.That(count, Is.EqualTo(14));
		Assert.That(min, Is.EqualTo(4));
		Assert.That(max, Is.EqualTo(17));

		count = _strings.MinMax(out String? minString, out String? maxString);
		Assert.That(count, Is.EqualTo(4));
		Assert.That(minString, Is.EqualTo("333"));
		Assert.That(maxString, Is.EqualTo("7777777"));
	}

	[Test]
	public void FindIndex() {
		Int32 idx = _strings.FindIndex(s => String.CompareOrdinal(s, "333") == 0);
		Assert.That(idx, Is.EqualTo(1));

		idx = Enumerable.Range(4, 14).FindIndex(i => i > 20);
		Assert.That(idx, Is.EqualTo(-1));
	}

	[Test]
	public void IndexOf() {
		Int32 idx = _strings.IndexOf("7777777");
		Assert.That(idx, Is.EqualTo(2));

		idx = Enumerable.Range(4, 14).IndexOf(555);
		Assert.That(idx, Is.EqualTo(-1));
	}

	[Test]
	public void ForEach() {
		Int32 counter = 0;
		Int32 valueSum = 0;
		Int32 indexSum = 0;
		Int32 expectedDataSum = 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13;
		Enumerable.Range(4, 10).ForEach(i => {
			++counter;
			valueSum += i;
		});
		Assert.That(counter, Is.EqualTo(10));
		Assert.That(valueSum, Is.EqualTo(expectedDataSum));

		counter = 0;
		valueSum = 0;
		Enumerable.Range(4, 10).ForEachIdx((i, idx) => {
			++counter;
			valueSum += i;
			indexSum += idx;
		});
		Assert.That(counter, Is.EqualTo(10));
		Assert.That(valueSum, Is.EqualTo(expectedDataSum));
		Assert.That(indexSum, Is.EqualTo(1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9));

		counter = 0;
		valueSum = 0;
		indexSum = 0;
		Enumerable
			.Range(4, 10)
			.Select((i, idx) => new { i, idx })
			.ToDictionary(obj => obj.idx, obj => obj.i)
			.ForEach((key, value) => {
				++counter;
				valueSum += value;
				indexSum += key;
			});

		Assert.That(counter, Is.EqualTo(10));
		Assert.That(valueSum, Is.EqualTo(expectedDataSum));
		Assert.That(indexSum, Is.EqualTo(1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9));
	}

	[Test]
	public void RandomElement() {
		Int32 elem = Enumerable.Range(4, 100).RandomElementOrDefault();
		Assert.That(elem, Is.GreaterThanOrEqualTo(4).And.LessThan(104));

		elem = Enumerable.Range(4, 1).RandomElementOrDefault();
		Assert.That(elem, Is.EqualTo(4));

		Int32[] elems = Enumerable.Range(4, 100).RandomElements(3);
		foreach (Int32 i in elems) {
			Assert.That(i, Is.GreaterThanOrEqualTo(4).And.LessThan(104));
		}
		
		Assert.That(Enumerable.Empty<String>().RandomElementOrDefault(), Is.Null);
		Assert.Throws<InvalidOperationException>(() =>Enumerable.Empty<String>().RandomElements(10));
	}

	[Test]
	public void WhereNotNull() {
		List<String> result = new[] { "string", null, "another" }.WhereNotNull().ToList();
		Assert.That(result, Has.Count.EqualTo(2));
		Assert.That(result, !Contains.Item(null));
	}
}