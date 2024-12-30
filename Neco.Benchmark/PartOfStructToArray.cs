namespace Neco.Benchmark;

using System.Linq;

public struct SomeStruct(Int32 id, Int32 someField, Int32 anotherField) {
	public readonly Int32 Id = id;
	public readonly Int32 SomeField = someField;
	public readonly Int32 AnotherField = anotherField;
}

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class PartOfStructToArray {
	private SomeStruct[] _data = null!;

	[Params(1024, 1024 * 1024)] 
	public Int32 N;

	[GlobalSetup]
	public void Setup() {
		_data = new SomeStruct[N];
		for (Int32 i = 0; i < N; i++) {
			_data[i] = new SomeStruct(Random.Shared.Next(), Random.Shared.Next(), Random.Shared.Next());
		}
	}

	[Benchmark]
	public Int32[] ByLinq() {
		return _data.Select(s => s.SomeField).ToArray();
	}

	[Benchmark]
	public Int32[] ForLoop() {
		// Small optimization so it's more like a parameter
		SomeStruct[] localCopy = _data;
		Int32[] retval = new Int32[localCopy.Length];
		for (Int32 i = 0; i < localCopy.Length; i++) {
			retval[i] = localCopy[i].SomeField;
		}

		return retval;
	}
	
	[Benchmark]
	public unsafe Int32[] BlockCopy() {
		const Int32 blockSize = 8;
		// Small optimization so it's more like a parameter
		SomeStruct[] localCopy = _data;
		Int32[] retval = new Int32[localCopy.Length];
		Int32* blockCopy = stackalloc Int32[blockSize];
		Int32 offset = 0;
		Int32 maxAvxOffset = localCopy.Length / blockSize;
		fixed (Int32* ptr = retval) {
			while (offset < maxAvxOffset) {
				blockCopy[0] = localCopy[offset].SomeField;
				blockCopy[1] = localCopy[offset+1].SomeField;
				blockCopy[2] = localCopy[offset+2].SomeField;
				blockCopy[3] = localCopy[offset+3].SomeField;
				blockCopy[4] = localCopy[offset+4].SomeField;
				blockCopy[5] = localCopy[offset+5].SomeField;
				blockCopy[6] = localCopy[offset+6].SomeField;
				blockCopy[7] = localCopy[offset+7].SomeField;
				Buffer.MemoryCopy(blockCopy, ptr+offset, blockSize, blockSize);
				offset += blockSize;
			}
		}

		for (Int32 i = offset; i < localCopy.Length; i++) {
			retval[i] = localCopy[i].SomeField;
		}
		return retval;
	}
}