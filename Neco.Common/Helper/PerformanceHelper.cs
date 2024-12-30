namespace Neco.Common.Helper;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Neco.Common.Extensions;

public static class PerformanceHelper {
	/// <summary>
	/// NO-Action used to quantify overhead.
	/// </summary>
	private static readonly Action _nopAction = () => { };

	/// <summary>
	/// Proper perf tests should be in a 'Benchmark.Net' project. This function is only useful as a rough guess into its performance.
	/// </summary>
	public static TimeSpan GetPerformanceRough<T>(String name, Action<T> testMe, T data) {
		return GetPerformanceRough(name, () => testMe(data));
	}

	/// <summary>
	/// Proper perf tests should be in a 'Benchmark.Net' project. This function is only useful as a rough guess into its performance.
	/// </summary>
	public static TimeSpan GetPerformanceRough(String name, Action testMe, Int32 warmupMs = 1000, Int32 testMs = 5000) {
		// Measure overhead
		Int64 nopExecutes = 0;
		GC.Collect(2, GCCollectionMode.Forced, true, true);
		Int32 gen0BeforeOverhead = GC.CollectionCount(0);
		Int32 gen1BeforeOverhead = GC.CollectionCount(1);
		Int32 gen2BeforeOverhead = GC.CollectionCount(2);
		Action nopAction = _nopAction;
		Int64 overheadTotalBytesAllocatedBefore = GC.GetTotalAllocatedBytes(true);
		Stopwatch sw = Stopwatch.StartNew();
		while (sw.Elapsed.TotalMilliseconds < 1000) {
			nopAction.Invoke();
			++nopExecutes;
		}

		sw.Stop();
		Int64 overheadTotalBytesAllocatedAfter = GC.GetTotalAllocatedBytes(true);
		Double overheadPerOperationMicroSeconds = sw.Elapsed.Ticks * 0.1 / nopExecutes;

		GC.Collect(2, GCCollectionMode.Forced, true, true);
		Int32 gen0Overhead = GC.CollectionCount(0) - gen0BeforeOverhead - 1;
		Int32 gen1Overhead = GC.CollectionCount(1) - gen1BeforeOverhead - 1;
		Int32 gen2Overhead = GC.CollectionCount(2) - gen2BeforeOverhead - 1;

		sw.Restart();
		// Warmup für JIT
		if (warmupMs > 0) {
			while (sw.Elapsed.TotalMilliseconds < warmupMs) {
				testMe.Invoke();
			}
		}

		GC.Collect(2, GCCollectionMode.Forced, true, true);
		Int32 gen0Before = GC.CollectionCount(0);
		Int32 gen1Before = GC.CollectionCount(1);
		Int32 gen2Before = GC.CollectionCount(2);
		Process currentProcess = Process.GetCurrentProcess();
		TimeSpan timeStart = currentProcess.TotalProcessorTime;

		// Test
		Int64 testExecutes = 0;
		Int64 totalBytesAllocatedBefore = GC.GetTotalAllocatedBytes(true);
		sw.Restart();
		while (sw.Elapsed.TotalMilliseconds < testMs) {
			testMe.Invoke();
			++testExecutes;
		}

		sw.Stop();
		Int64 totalBytesAllocatedAfter = GC.GetTotalAllocatedBytes(true);
		TimeSpan timeEnd = currentProcess.TotalProcessorTime;
		GC.Collect(2, GCCollectionMode.Forced, true, true);
		Int32 gen0 = GC.CollectionCount(0);
		Int32 gen1 = GC.CollectionCount(1);
		Int32 gen2 = GC.CollectionCount(2);

		Double timeInMicroseconds = sw.Elapsed.Ticks * 0.1;
		Double timePerOperationMicroSeconds = timeInMicroseconds / testExecutes;
		Double cleanTimePerOperationMicroSeconds = timePerOperationMicroSeconds - overheadPerOperationMicroSeconds;
		Double operationsPerSecond = 1_000_000D / cleanTimePerOperationMicroSeconds;
		TimeSpan totalCpu = (timeEnd - timeStart);
		Double totalCpuAdjustedOperationsPerSecond = 1D / ((totalCpu.TotalSeconds / testExecutes) - overheadPerOperationMicroSeconds / 1_000_000D);
		TimeSpan totalCleanTime = TimeSpan.FromTicks((Int64)(cleanTimePerOperationMicroSeconds * testExecutes * 10));

		Double overheadBytesAllocatedPerRun = (overheadTotalBytesAllocatedAfter - overheadTotalBytesAllocatedBefore) / (Double)nopExecutes;
		Double bytesAllocatedPerRun = (totalBytesAllocatedAfter - totalBytesAllocatedBefore) / (Double)testExecutes;
		Double bytesAllocatedPerRunClean = bytesAllocatedPerRun - overheadBytesAllocatedPerRun;

		Console.WriteLine($"{name} {testExecutes:n0} ops in {sw.Elapsed.TotalMilliseconds:n3}ms = clean per operation: {cleanTimePerOperationMicroSeconds:n3}µs or {operationsPerSecond:n3}op/s with {bytesAllocatedPerRunClean.ToFileSize()} per run and GC {gen0 - gen0Before - 1 - gen0Overhead}/{gen1 - gen1Before - 1 - gen1Overhead}/{gen2 - gen2Before - 1 - gen2Overhead}");
		Console.WriteLine($"{name} TotalCPUTime per operation: {totalCpu.TotalMilliseconds:n3}ms or clean {totalCpuAdjustedOperationsPerSecond:n3}op/s for a factor of {totalCpu.TotalMilliseconds / sw.Elapsed.TotalMilliseconds:n3}");
		return totalCleanTime;
	}
}