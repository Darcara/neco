namespace Neco.Benchmark;

using System.Linq;
using CommunityToolkit.HighPerformance;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class ArrayLayout {
	private Byte[,] _multiDimensional;
	private Byte[][] _jagged;
	private Byte[] _contiguousRowByColumn;
	private Byte[] _contiguousColumnByRow;
	private Int32 _width;
	private Int32 _height;

	[Params("1000x1000", "10_000x100", "100x10_000")]
	public String Layout { get; set; }

	[GlobalSetup]
	public void GlobalSetup() {
		_width = Int32.Parse(Layout.Split('x').First().Replace("_", String.Empty));
		_height = Int32.Parse(Layout.Split('x').Last().Replace("_", String.Empty));
		_multiDimensional = new Byte[_width, _height];
		_jagged = new Byte[_width][];
		_contiguousRowByColumn = new Byte[_width * _height];
		_contiguousColumnByRow = new Byte[_width * _height];

		for (Int32 i = 0; i < _width; i++) {
			_jagged[i] = new Byte[_height];
			Random.Shared.NextBytes(_jagged[i]);

			for (Int32 j = 0; j < _height; j++) {
				_multiDimensional[i, j] = _jagged[i][j];
				_contiguousRowByColumn[i * _height + j] = _jagged[i][j];
				_contiguousColumnByRow[j * _width + i] = _jagged[i][j];
			}
		}
	}

	[Benchmark]
	public Int64 Jagged() {
		Byte[][] localCopy = _jagged;
		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			for (Int32 j = 0; j < _height; j++) {
				result += localCopy[i][j];
			}
		}

		return result;
	}

	[Benchmark]
	public Int64 MultiDimensional() {
		Byte[,] localCopy = _multiDimensional;
		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			for (Int32 j = 0; j < _height; j++) {
				result += localCopy[i, j];
			}
		}

		return result;
	}

	[Benchmark]
	public Int64 ContiguousRowByColumn() {
		Byte[] localCopy = _contiguousRowByColumn;
		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			for (Int32 j = 0; j < _height; j++) {
				result += localCopy[i * _height + j];
			}
		}

		return result;
	}
	
	[Benchmark]
	public Int64 ContiguousRowByColumnCachedRow() {
		Byte[] localCopy = _contiguousRowByColumn;
		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			Int32 rowBegin = i * _height;
			for (Int32 j = 0; j < _height; j++) {
				result += localCopy[rowBegin + j];
			}
		}

		return result;
	}

	[Benchmark]
	public Int64 ContiguousColumnByRow() {
		Byte[] localCopy = _contiguousColumnByRow;
		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			for (Int32 j = 0; j < _height; j++) {
				result += localCopy[j * _width + i];
			}
		}

		return result;
	}
	
	[Benchmark]
	public Int64 Span2DMultidimensional() {
		Span2D<Byte> span = new(_multiDimensional);

		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			for (Int32 j = 0; j < _height; j++) {
				result += span[i, j];
			}
		}

		return result;
	}
	
	[Benchmark]
	public Int64 Span2DRowByColumn() {
		Span2D<Byte> span = new(_contiguousRowByColumn, _width, _height);

		Int64 result = 0;
		for (Int32 i = 0; i < _width; i++) {
			for (Int32 j = 0; j < _height; j++) {
				result += span[i, j];
			}
		}

		return result;
	}
}