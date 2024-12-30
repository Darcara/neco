namespace Neco.Benchmark;

using System.Runtime.InteropServices;
using System.Text;
using Neco.Common;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class Utf8ToString {
	[Params(100, 4096, MagicNumbers.MaxNonLohBufferSize)]
	public Int32 N;

	private Byte[] _bytes = null!;

	[GlobalSetup]
	public void Setup() {
		_bytes = new Byte[N];
		Random.Shared.NextBytes(_bytes);
	}

	[Benchmark]
	public String ByEncoding() {
		return Encoding.UTF8.GetString(_bytes);
	}

	[Benchmark]
	public unsafe String ByEncodingPtr() {
		fixed (Byte* ptr = _bytes) {
			return Encoding.UTF8.GetString(ptr, _bytes.Length);
		}
	}

	[Benchmark]
	public unsafe String ByMarshal() {
		fixed (Byte* ptr = _bytes) {
			return Marshal.PtrToStringUTF8(new IntPtr(ptr), _bytes.Length);
		}
	}
}