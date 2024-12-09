namespace Neco.BenchmarkLibrary.Config;

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

public abstract class BaseConfig : ManualConfig{
	protected BaseConfig(params String[]? category) {
		Add(DefaultConfig.Instance);
		((List<IValidator>)GetValidators()).Remove(JitOptimizationsValidator.FailOnError);
		AddValidator(JitOptimizationsValidator.DontFailOnError);
		AddValidator(ReturnValueValidator.DontFailOnError);

		AddDiagnoser(MemoryDiagnoser.Default);
		// AddDiagnoser(ThreadingDiagnoser.Default);
		
		// Disabled until fixed: https://github.com/dotnet/BenchmarkDotNet/issues/1520
		// AddHardwareCounters(HardwareCounter.BranchInstructions);
		// AddHardwareCounters(HardwareCounter.BranchMispredictions);
		// AddHardwareCounters(HardwareCounter.CacheMisses);
		// AddHardwareCounters(HardwareCounter.LlcMisses);
		// AddHardwareCounters(HardwareCounter.TotalCycles);
		// AddHardwareCounters(HardwareCounter.UnhaltedCoreCycles);
		
		AddColumn(new RelativeBaselineColumn());
		AddColumn(new RelativeErrorColumn());
		AddColumn(new RelativeAllocatedColumn());
		AddColumn(StatisticColumn.OperationsPerSecond);
		AddColumn(StatisticColumn.Median);
		AddColumn(StatisticColumn.P95);
		AddExporter(PlainExporter.Default);
		if (!GetExporters().Any(exp => exp is HtmlExporter)) AddExporter(HtmlExporter.Default);

		if (category != null && category.Length > 0)
			AddFilter(new AnyCategoriesFilter(category));

		WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));

		if (!GetLoggers().Any()) AddLogger(new ConsoleLogger());

		// DontOverwriteResults will create a subdirectory [date]-[time] on every run.
		// If not specified, results directory will be used (and old benchmarks overwritten)
		// Options |= ConfigOptions.DontOverwriteResults;
		Options |= ConfigOptions.DisableOptimizationsValidator;

		WithSummaryStyle(new SummaryStyle(null, false, null, null, textColumnJustification: SummaryTable.SummaryTableColumn.TextJustification.Right));
	}

	protected static Job CreateDefaultJob() {
		Job job = Job.Default
				.WithoutEnvironmentVariables()
				.WithPlatform(Platform.X64)
				.DontEnforcePowerPlan()
				.WithGcConcurrent(true)
				.WithGcForce(true)
				.WithGcServer(true)
			;

		if (Environment.ProcessorCount > 1) {
			// Must use at least 2 cores for concurrent server gc
			// Only use last two cores: 0b11000000...
			// The last 2 cores might be E/C cores, which will result in slightly slower benchmarks
			// Will result in sligtly faster benchmarks, because of reduced context switching
			nint affinityMask = (1 << Environment.ProcessorCount - 1) | (1 << Environment.ProcessorCount - 2);
			job = job.WithAffinity(affinityMask);
		}
		return job;
	}
}