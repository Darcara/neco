namespace Neco.Benchmark.Config;

using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

public class RelativeBaselineColumn : IColumn {
	#region Implementation of IColumn

	public String GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) {
		String? logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
		BenchmarkCase index = summary.BenchmarksCases
			.Where(b => summary.GetLogicalGroupKey(b) == logicalGroupKey)
			.Where(b => summary[b] != null && summary[b].ResultStatistics != null)
			.OrderBy(b => summary[b].ResultStatistics.Mean)
			.FirstOrDefault();

		if (index == null || summary[index] == null || summary[index].ResultStatistics == null || summary[benchmarkCase] == null || summary[benchmarkCase].ResultStatistics == null)
			return "?";

		Statistics baselineStatistics = summary[index].ResultStatistics;
		Statistics caseStatistics = summary[benchmarkCase].ResultStatistics;

		Double val;
		if (baselineStatistics == caseStatistics) val = 1.0;
		else val = caseStatistics.Mean / baselineStatistics.Mean;

		return val.ToString("N3");
	}

	public String GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

	public Boolean IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

	public Boolean IsAvailable(Summary summary) => true;

	public String Id { get; } = nameof(RelativeBaselineColumn);
	public String ColumnName { get; } = "Scaled";
	public Boolean AlwaysShow { get; } = true;
	public ColumnCategory Category { get; } = ColumnCategory.Baseline;
	public Int32 PriorityInCategory { get; } = 0;
	public Boolean IsNumeric { get; } = true;
	public UnitType UnitType { get; } = UnitType.Dimensionless;
	public String Legend { get; } = "Mean(CurrentBenchmark) / Smallest mean";

	#endregion

	public override String ToString() => ColumnName;
}