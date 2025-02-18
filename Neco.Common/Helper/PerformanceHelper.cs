namespace Neco.Common.Helper;

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
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
		TimeSpan totalCpu = timeEnd - timeStart;
		Double totalCpuAdjustedOperationsPerSecond = 1D / (totalCpu.TotalSeconds / testExecutes - overheadPerOperationMicroSeconds / 1_000_000D);
		TimeSpan totalCleanTime = TimeSpan.FromTicks((Int64)(cleanTimePerOperationMicroSeconds * testExecutes * 10));

		Double overheadBytesAllocatedPerRun = (overheadTotalBytesAllocatedAfter - overheadTotalBytesAllocatedBefore) / (Double)nopExecutes;
		Double bytesAllocatedPerRun = (totalBytesAllocatedAfter - totalBytesAllocatedBefore) / (Double)testExecutes;
		Double bytesAllocatedPerRunClean = bytesAllocatedPerRun - overheadBytesAllocatedPerRun;

		Console.WriteLine($"{name} {testExecutes:n0} ops in {sw.Elapsed.TotalMilliseconds:n3}ms = clean per operation: {cleanTimePerOperationMicroSeconds:n3}µs or {operationsPerSecond:n3}op/s with {bytesAllocatedPerRunClean.ToFileSize()} per run and GC {gen0 - gen0Before - 1 - gen0Overhead}/{gen1 - gen1Before - 1 - gen1Overhead}/{gen2 - gen2Before - 1 - gen2Overhead}");
		Console.WriteLine($"{name} TotalCPUTime per operation: {totalCpu.TotalMilliseconds:n3}ms or clean {totalCpuAdjustedOperationsPerSecond:n3}op/s for a factor of {totalCpu.TotalMilliseconds / sw.Elapsed.TotalMilliseconds:n3}");
		return totalCleanTime;
	}

	/// <summary>
	/// Estimates the size of an object, by creating it and asking the <see cref="GC"/> how much memory is alloceted. 
	/// </summary>
	/// <param name="data">Data to be passed to the creation function</param>
	/// <param name="creator">The creation function creates the object to estimate and returns it</param>
	/// <typeparam name="TCreated">The type of the created object</typeparam>
	/// <typeparam name="TData">The type </typeparam>
	public static GenerationResult<TCreated> EstimateObjectSize<TData, TCreated>(TData data, Func<TData, TCreated> creator) where TCreated : class => EstimateObjectSize(null, 1, 1, Console.Out, data, creator);

	/// <summary>
	/// Estimates the size of an object, by creating it and asking the <see cref="GC"/> how much memory is alloceted. 
	/// </summary>
	/// <param name="name">A name for for this performance test</param>
	/// <param name="data">Data to be passed to the creation function</param>
	/// <param name="creator">The creation function creates the object to estimate and returns it</param>
	/// <typeparam name="TCreated">The type of the created object</typeparam>
	/// <typeparam name="TData">The type </typeparam>
	public static GenerationResult<TCreated> EstimateObjectSize<TData, TCreated>(String name, TData data, Func<TData, TCreated> creator) where TCreated : class => EstimateObjectSize(name, 1, 1, Console.Out, data, creator);

	/// <summary>
	/// </summary>
	/// <param name="name">A name for for this performance test</param>
	/// <param name="warmupInterations">Number of creations that are not measued. Should be at least one, so caches and one-time initializers can run without being measured</param>
	/// <param name="measureIterations">Number of creations to average, must be at least 1</param>
	/// <param name="outputResults"></param>
	/// <param name="data"></param>
	/// <param name="creator"></param>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="TCreated"></typeparam>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
	public static GenerationResult<TCreated> EstimateObjectSize<TData, TCreated>(String? name, Int32 warmupInterations, Int32 measureIterations, TextWriter? outputResults, TData data, Func<TData, TCreated> creator) where TCreated : class {
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measureIterations);
		warmupInterations = Math.Max(0, warmupInterations);

		for (Int32 i = 0; i < warmupInterations; i++) {
			TCreated warmupObject = creator(data);
			if (warmupObject is IDisposable disposable) disposable.Dispose();
			else if (warmupObject is IAsyncDisposable asyncDisposable) asyncDisposable.DisposeAsync().GetResultBlocking();
		}

		Stopwatch sw = new();
		Process currentProcess = Process.GetCurrentProcess();
		Int64[] allocatedBefore = new Int64[measureIterations];
		Int64[] allocatedAfter = new Int64[measureIterations];
		Int64[] totalBytesAllocatedBefore = new Int64[measureIterations];
		Int64[] totalBytesAllocatedAfter = new Int64[measureIterations];
		Int64[] processBytesBefore = new Int64[measureIterations];
		Int64[] processBytesAfter = new Int64[measureIterations];
		TimeSpan[] duration = new TimeSpan[measureIterations];

		for (Int32 i = 0; i < measureIterations; i++) {
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
			Thread.Sleep(250);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			Thread.Sleep(250);
			GC.WaitForPendingFinalizers();
			allocatedBefore[i] = GC.GetTotalMemory(true);
			totalBytesAllocatedBefore[i] = GC.GetTotalAllocatedBytes(true);
			Thread.Sleep(250);
			currentProcess.Refresh();
			processBytesBefore[i] = currentProcess.PrivateMemorySize64;
			sw.Restart();
			
			MeasureSingleCreation(data, creator, i, currentProcess, sw, duration, totalBytesAllocatedAfter, allocatedAfter, processBytesAfter);
		}

		TCreated created = creator(data);
		GenerationResult<TCreated> result = new() {
			Name = name ?? created.GetType().GetGenericName(),
			AllocateDifference = allocatedBefore.Zip(allocatedAfter).Select(((Int64 before, Int64 after) tpl) => tpl.after - tpl.before).Average(),
			TotalAllocatedDifference = totalBytesAllocatedBefore.Zip(totalBytesAllocatedAfter).Select(((Int64 before, Int64 after) tpl) => tpl.after - tpl.before).Average(),
			ProcessBytesDifference = processBytesBefore.Zip(processBytesAfter).Select(((Int64 before, Int64 after) tpl) => tpl.after - tpl.before).Average(),
			ElapsedTime = TimeSpan.FromMilliseconds(duration.Average(ts => ts.TotalMilliseconds)),
			CreatedObject = created,
		};
		outputResults?.WriteLine(result);

		return result;
	}

	[MethodImpl(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
	private static void MeasureSingleCreation<TData, TCreated>(TData data, Func<TData, TCreated> creator, Int32 i, Process currentProcess, Stopwatch sw, TimeSpan[] duration, Int64[] totalBytesAllocatedAfter, Int64[] allocatedAfter, Int64[] processBytesAfter) where TCreated : class {
		TCreated createdObject = creator(data);

		sw.Stop();
		duration[i] = sw.Elapsed;
		totalBytesAllocatedAfter[i] = GC.GetTotalAllocatedBytes(true);
		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
		Thread.Sleep(250);
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		Thread.Sleep(250);
		GC.WaitForPendingFinalizers();
		allocatedAfter[i] = GC.GetTotalMemory(true);
		Thread.Sleep(500);
		currentProcess.Refresh();
		processBytesAfter[i] = currentProcess.PrivateMemorySize64;
			
		if (createdObject is IDisposable disposable) disposable.Dispose();
		else if (createdObject is IAsyncDisposable asyncDisposable) asyncDisposable.DisposeAsync().GetResultBlocking();
		createdObject = null!;
	}
}

public class GenerationResult {
	public required String Name { get; init; }
	public required Double AllocateDifference { get; init; }
	public required Double TotalAllocatedDifference { get; init; }
	public required Double ProcessBytesDifference { get; init; }
	public required TimeSpan ElapsedTime { get; init; }

	/// <inheritdoc />
	public override String ToString() => $"{Name} built with {AllocateDifference.ToFileSize()} in {ElapsedTime}. During the creation {TotalAllocatedDifference.ToFileSize()} were allocated. Process size increased by {ProcessBytesDifference.ToFileSize()}.";

	public static implicit operator String(GenerationResult gr) => gr.ToString();
}

public sealed class GenerationResult<T> : GenerationResult, IDisposable, IAsyncDisposable {
	public required T CreatedObject { get; init; }

	public static implicit operator T(GenerationResult<T> gr) => gr.CreatedObject;

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		if (CreatedObject is IDisposable disposable)
			disposable.Dispose();
		else if (CreatedObject is IAsyncDisposable asyncDisposable)
			asyncDisposable.DisposeAsync().GetResultBlocking();
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync() {
		if (CreatedObject is IAsyncDisposable asyncDisposable)
			return asyncDisposable.DisposeAsync();
		if (CreatedObject is IDisposable disposable)
			disposable.Dispose();
		return ValueTask.CompletedTask;
	}

	#endregion
}