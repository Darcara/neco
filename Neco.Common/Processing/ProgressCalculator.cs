namespace Neco.Common.Processing;

using System.Diagnostics;
using System.Globalization;
using Neco.Common.Extensions;

public class ProgressCalculator {
	private readonly Func<String>? _prefix;
	private readonly Func<String>? _suffix;
	private readonly Stopwatch _timeTaken = new();
	private readonly TimeSpan _timeBetweenLogs;
	private Int64 _itemsCompleted;
	private Int64 _totalItems;
	private Int64 _lastLog;
	private Int32 _lastLogLine = Int32.MinValue;

	public TimeSpan TimeSinceLastLog => Stopwatch.GetElapsedTime(_lastLog);
	public Boolean ShouldLog => TimeSinceLastLog >= _timeBetweenLogs;
	public Int64 CompletedItems => _itemsCompleted;
	public Int64 TotalItems => _totalItems;

	public ProgressCalculator(TimeSpan? timeBetweenLogs = null, Func<String>? prefix = null, Func<String>? suffix = null) {
		_timeBetweenLogs = timeBetweenLogs ?? TimeSpan.FromSeconds(1);
		_prefix = prefix;
		_suffix = suffix;
	}

	public ProgressCalculator Start(Int64 totalItems) {
		_totalItems = totalItems;
		_timeTaken.Start();
		return this;
	}

	public void Stop() {
		_timeTaken.Stop();
	}

	public void Reset() {
		_timeTaken.Reset();
		_itemsCompleted = 0;
		_totalItems = 0;
		_lastLog = 0;
	}

	public void Restart(Int64 newMaxItems = -1) {
		Stop();
		Reset();
		Start(newMaxItems <= 0 ? _totalItems : newMaxItems);
	}

	public void CompleteItems(Int64 count = 1) {
		_itemsCompleted += count;
		if (_totalItems < _itemsCompleted)
			_totalItems = _itemsCompleted;
	}

	public static String SimpleTimestamp() => DateTime.UtcNow.ToString("HH:mm:ss.fff> ", CultureInfo.InvariantCulture);

	public String Log() {
		_lastLog = Stopwatch.GetTimestamp();
		Double percent = _totalItems > 0 ? (Double)_itemsCompleted / _totalItems : 0;
		TimeSpan elapsed = _timeTaken.Elapsed;
		TimeSpan estimatedTotal = _totalItems > 0 && _itemsCompleted > 0 ? TimeSpan.FromTicks((Int64)(elapsed.Ticks / percent)) : elapsed;
		TimeSpan remaining = estimatedTotal - elapsed;
		return $"{(_prefix == null ? String.Empty : _prefix())}Progress: {percent:P2} ({_itemsCompleted}/{_totalItems}), Elapsed: {elapsed.ToReadableString()}, Remaining: {remaining.ToReadableString()}, Total Estimated: {estimatedTotal.ToReadableString()}, Until: {DateTime.UtcNow + remaining:s}{(_suffix == null ? String.Empty : _suffix())}";
	}

	public void LogToConsole(Boolean force = false, Boolean startOfLine = false) {
		if (!force && !ShouldLog) return;

		(Int32 left, Int32 top) = Console.GetCursorPosition();
		if (startOfLine && left == 0 && _lastLogLine == top - 1) {
			Console.SetCursorPosition(0, top - 1);
			top -= 1;
		}

		_lastLogLine = top;
		Console.WriteLine($"{_lastLogLine}:{Console.BufferWidth}x{Console.BufferHeight}:{Console.WindowWidth}x{Console.WindowHeight} "+Log());
	}
}