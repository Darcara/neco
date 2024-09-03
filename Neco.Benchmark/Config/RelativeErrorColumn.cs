namespace Neco.Benchmark.Config;

using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Mathematics.Common;

public class RelativeErrorColumn : IColumn {
	#region Implementation of IColumn

	public String GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) {
		Statistics? s = summary[benchmarkCase].ResultStatistics;
		if (s == null) return "NA";
		Double margin = new ConfidenceInterval(s.Mean, s.StandardError, s.N, ConfidenceLevel.L90).Margin;
		return ((margin / s.Mean) * 1000).ToString("N3");
	}

	public String GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

	public Boolean IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

	public Boolean IsAvailable(Summary summary) => true;

	public String Id => nameof(RelativeErrorColumn);
	public String ColumnName => "RelError";
	public Boolean AlwaysShow => true;
	public ColumnCategory Category => ColumnCategory.Statistics;
	public Int32 PriorityInCategory => 0;
	public Boolean IsNumeric => true;
	public UnitType UnitType => UnitType.Dimensionless;
	public String Legend => "Half of 99.9% confidence interval(error) relative to the mean, multiplied by 1000";

	#endregion

	public override String ToString() => ColumnName;
}