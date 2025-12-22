namespace Neco.Benchmark;

using System.Numerics;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class NextPowerOfTwo {
	[Params(15, 16, 17)] public Int32 Query { get; set; }

	// From: https://stackoverflow.com/a/35485579/423083
	// This is the Software fallback for BitOperations.Log2
	[Benchmark]
	public Int32 BitShift() {
		Int32 v = Query;
		v--;
		v |= v >> 1;
		v |= v >> 2;
		v |= v >> 4;
		v |= v >> 8;
		v |= v >> 16;
		v++;
		return v;
	}

	// From: https://stackoverflow.com/a/67790380/423083
	[Benchmark]
	public Int32 BitOpsLog2() {
		UInt32 v = (UInt32)Query;
		return 1 << (BitOperations.Log2(v - 1) + 1); // or
	}

	// From: https://stackoverflow.com/a/67790380/423083
	[Benchmark]
	public Int32 BitOpsLeadingZeroCount() {
		UInt32 v = (UInt32)Query;
		return 1 << (31 - BitOperations.LeadingZeroCount(v - 1) + 1);
	}

	// From: https://stackoverflow.com/a/35478829/423083
	[Benchmark]
	public Int32 BasicMath() {
		Int32 v = Query;
		return (Int32)Math.Pow(2, Math.Ceiling(Math.Log2(v)));
	}
}