namespace Neco.BenchmarkLibrary.Config;

using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

public class RelativeAllocatedColumn : IColumn {
	#region Implementation of IColumn

	public String GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) {
		String? logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
		BenchmarkCase? index = summary.BenchmarksCases
			.Where(b => summary.GetLogicalGroupKey(b) == logicalGroupKey)
			.Where(b => summary[b].GcStats.GetBytesAllocatedPerOperation(summary[b].BenchmarkCase) != null)
			.Where(b => summary[b].GcStats.GetBytesAllocatedPerOperation(summary[b].BenchmarkCase) > 0)
			.MinBy(b => summary[b].GcStats.GetBytesAllocatedPerOperation(summary[b].BenchmarkCase));

		if (index == null || summary[index].GcStats.GetBytesAllocatedPerOperation(index) == null || summary[benchmarkCase].GcStats.GetBytesAllocatedPerOperation(benchmarkCase) == null)
			return "-";

		Int64 baselineStatistics = summary[index].GcStats.GetBytesAllocatedPerOperation(index) ?? 0;
		Int64 caseStatistics = summary[benchmarkCase].GcStats.GetBytesAllocatedPerOperation(benchmarkCase) ?? 0;

		Double val;
		if (caseStatistics == 0) return "-";
		if (baselineStatistics == caseStatistics) val = 1.0;
		else val = caseStatistics / (Double)baselineStatistics;
		return val.ToString("N3");
	}

	public String GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

	public Boolean IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

	public Boolean IsAvailable(Summary summary) => summary.Reports.Any(r => r.GcStats.GetBytesAllocatedPerOperation(r.BenchmarkCase) != null);

	public String Id => nameof(RelativeAllocatedColumn);
	public String ColumnName => "AllocScaled";
	public Boolean AlwaysShow => true;
	public ColumnCategory Category => ColumnCategory.Metric;
	public Int32 PriorityInCategory => GC.MaxGeneration+2;
	public Boolean IsNumeric => true;
	public UnitType UnitType => UnitType.Dimensionless;
	public String Legend => "Allocated(CurrentBenchmark) / Smallest allocated";

	#endregion

	public override String ToString() => ColumnName;
}