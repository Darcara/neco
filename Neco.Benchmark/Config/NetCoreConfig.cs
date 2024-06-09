namespace Neco.Benchmark.Config;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
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
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

public class NetCoreConfig : ManualConfig {
	public NetCoreConfig() : this(null) {
	}

	public NetCoreConfig(params String[]? category) {
		Add(DefaultConfig.Instance);
		((List<IValidator>)GetValidators()).Remove(JitOptimizationsValidator.FailOnError);
		AddValidator(JitOptimizationsValidator.DontFailOnError);
		AddJob(Job
			.Default
			.WithGcConcurrent(true)
			.WithGcServer(true)
			.WithGcForce(true)
			.DontEnforcePowerPlan()
			.WithAffinity(new IntPtr(0b00000000_00000011))
			.WithPlatform(Platform.X64)
			.WithRuntime(CoreRuntime.Core80));

		AddDiagnoser(MemoryDiagnoser.Default);
		// AddDiagnoser(ThreadingDiagnoser.Default);
		AddHardwareCounters(HardwareCounter.BranchInstructions);
		AddHardwareCounters(HardwareCounter.BranchMispredictions);
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
		AddExporter(HtmlExporter.Default);
		AddExporter(PlainExporter.Default);

		if (category != null && category.Length > 0)
			AddFilter(new AnyCategoriesFilter(category));

		WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));

		if (!GetLoggers().Any()) AddLogger(new ConsoleLogger());

		Options |= ConfigOptions.KeepBenchmarkFiles;
		Options |= ConfigOptions.DisableOptimizationsValidator;

		WithSummaryStyle(new SummaryStyle(null, false,  null,  null, textColumnJustification: SummaryTable.SummaryTableColumn.TextJustification.Right));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="benchmarkType">Type that contains the <see cref="BenchmarkDotNet.Attributes.BenchmarkAttribute"/> and <see cref="BenchmarkCategoryAttribute"/></param>
	/// <param name="config">The configuration to use. Usually <see cref="NetCoreConfig"/></param>
	/// <param name="resultsSuffix">Suffix of the results folder</param>
	/// <returns>The benchmark-<see cref="Summary"/></returns>
	public static Summary Run(Type benchmarkType, IConfig config, String? resultsSuffix = null) {
		Summary summary = BenchmarkRunner.Run(benchmarkType, config);

		if (!String.IsNullOrWhiteSpace(resultsSuffix)) {
			String destDirName = Path.ChangeExtension(summary.ResultsDirectoryPath, resultsSuffix);

			if (Directory.Exists(destDirName))
				Directory.Delete(destDirName, true);
			Directory.Move(summary.ResultsDirectoryPath, destDirName);

			String logSource = Path.Combine(config.ArtifactsPath, $"{summary.Title}.log");
			// if (File.Exists(logSource))
			File.Move(logSource, Path.Combine(destDirName, $"{summary.Title}.log"));
		}

		return summary;
	}

	public static Summary Run<TBench, TConfig>(params String[]? category) where TConfig : NetCoreConfig, new() {
		TConfig? config = new();
		if (category != null && category.Length > 0)
			config.Add(new AnyCategoriesFilter(category));

		return Run(typeof(TBench), config, String.Join("+", category));
	}
}