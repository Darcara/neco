namespace Neco.Benchmark;

using Neco.Common;
using Neco.Common.Data.Hash;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class Hashing {

	[Params(100, 4092, MagicNumbers.MaxNonLohBufferSize)]
	public Int32 N;

	private Byte[] _data = null!;
	
	[GlobalSetup]
	public void Setup() {
		_data = new Byte[N];
		Random.Shared.NextBytes(_data);
	}
	
	[Benchmark]
	[Obsolete("Obsolete")]
	public UInt64 WyHash3() {
		return WyHashFinal3.HashOneOffLong(_data);
	}
	
	[Benchmark]
	public UInt64 XxHash3() {
		return System.IO.Hashing.XxHash3.HashToUInt64(_data);
	}
	
	[Benchmark]
	public UInt64 XxHash64() {
		return System.IO.Hashing.XxHash64.HashToUInt64(_data);
	}
}