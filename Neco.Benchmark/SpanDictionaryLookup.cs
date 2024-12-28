namespace Neco.Benchmark;

using System.Collections.Frozen;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class SpanDictionaryLookup {
	private Dictionary<String, Int64> _regularLookup = null!;
	private Dictionary<String, Int64>.AlternateLookup<ReadOnlySpan<Char>> _regularLookupAlt;
	private FrozenDictionary<String, Int64> _frozenLookup = null!;
	private FrozenDictionary<String, Int64>.AlternateLookup<ReadOnlySpan<Char>> _frozenLookupAlt;
	private String _lookMeUp = null!;
	private String _lookMeUpWithJunk = null!;

	[Params(true, false)] 
	public Boolean Hit;

	[GlobalSetup]
	public void Setup() {
		_regularLookup = new Dictionary<String, Int64>(StringComparer.Ordinal);
		while (_regularLookup.Count < 50_000) {
			_regularLookup.Add(Guid.NewGuid().ToString(), Random.Shared.NextInt64());
		}

		if (Hit) {
			_lookMeUp = _regularLookup.Skip(25_000).First().Key;
		} else {
			do {
				_lookMeUp = Guid.NewGuid().ToString();
			} while (_regularLookup.ContainsKey(_lookMeUp));
		}

		_lookMeUpWithJunk = "ABC" + _lookMeUp + "123";
		_frozenLookup = _regularLookup.ToFrozenDictionary(StringComparer.Ordinal);

		_regularLookupAlt = _regularLookup.GetAlternateLookup<ReadOnlySpan<Char>>();
		_frozenLookupAlt = _frozenLookup.GetAlternateLookup<ReadOnlySpan<Char>>();
	}

	[Benchmark]
	public Int64 StringLookupRegular() {
		_regularLookup.TryGetValue(_lookMeUp, out Int64 value);
		return value;
	}

	[Benchmark]
	public Int64 StringLookupFrozen() {
		_frozenLookup.TryGetValue(_lookMeUp, out Int64 value);
		return value;
	}

	// we have to create the span here, but want to mitigate the impact on the benchmark
	[Benchmark(OperationsPerInvoke = 10_000)]
	public Int64 SpanLookupRegular() {
		ReadOnlySpan<Char> readOnlySpan = _lookMeUpWithJunk.AsSpan(3, 36);
		var lookup = _regularLookupAlt;
		Int64 value = 0;
		for (Int32 i = 0; i < 10_000; i++) {
			lookup.TryGetValue(readOnlySpan, out value);
		}

		return value;
	}

	// we have to create the span here, but want to mitigate the impact on the benchmark
	[Benchmark(OperationsPerInvoke = 10_000)]
	public Int64 SpanLookupFrozen() {
		ReadOnlySpan<Char> readOnlySpan = _lookMeUpWithJunk.AsSpan(3, 36);
		var lookup = _frozenLookupAlt;
		Int64 value = 0;
		for (Int32 i = 0; i < 10_000; i++) {
			lookup.TryGetValue(readOnlySpan, out value);
		}

		return value;
	}
}