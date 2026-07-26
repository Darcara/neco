namespace Neco.Web.HtmlParsing;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public enum Severity {
	Trace = 0,
	Warn,
	Error,
}

public readonly struct LogEntry : IEquatable<LogEntry>, IComparable<LogEntry>, IComparable {
	public Severity Severity { get; init; }
	public DateTime Time { get; init; }
	public String Message { get; init; }

	#region Overrides of ValueType

	/// <inheritdoc />
	public override String ToString() => $"{Time:yyyy-MM-dd HH:mm:ss.fff} [{Severity,5}]: {Message}";

	#endregion

	#region Relational members

	/// <inheritdoc />
	public Int32 CompareTo(LogEntry other) => Time.CompareTo(other.Time);

	/// <inheritdoc />
	public Int32 CompareTo(Object? obj) {
		if (obj is null) return 1;
		return obj is LogEntry other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(LogEntry)}");
	}

	public static Boolean operator <(LogEntry left, LogEntry right) => left.CompareTo(right) < 0;

	public static Boolean operator >(LogEntry left, LogEntry right) => left.CompareTo(right) > 0;

	public static Boolean operator <=(LogEntry left, LogEntry right) => left.CompareTo(right) <= 0;

	public static Boolean operator >=(LogEntry left, LogEntry right) => left.CompareTo(right) >= 0;

	#endregion

	#region Equality members

	/// <inheritdoc />
	public Boolean Equals(LogEntry other) => Severity == other.Severity && Time.Equals(other.Time) && Message == other.Message;

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => obj is LogEntry other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() => HashCode.Combine((Int32)Severity, Time, Message);

	public static Boolean operator ==(LogEntry left, LogEntry right) => left.Equals(right);

	public static Boolean operator !=(LogEntry left, LogEntry right) => !left.Equals(right);

	#endregion
}

public interface IParseLog {
	public IReadOnlyList<LogEntry> Entries { get; }

	public void Trace(String msg);

	public void Warn(String msg);

	public void Error(String msg);
}

public sealed class NoLog : IParseLog {
	public static readonly NoLog Instance = new();

	#region Implementation of IParseLog

	/// <inheritdoc />
	public IReadOnlyList<LogEntry> Entries => [];

	/// <inheritdoc />
	public void Trace(String msg) {
	}

	/// <inheritdoc />
	public void Warn(String msg) {
	}

	/// <inheritdoc />
	public void Error(String msg) {
	}

	#endregion
}

public sealed class ParseLog : IParseLog {
	private readonly ConcurrentBag<LogEntry> _logEntries = new();
	public IReadOnlyList<LogEntry> Entries => _logEntries.Order().ToList();

	public void Trace(String msg) {
		_logEntries.Add(new LogEntry { Severity = Severity.Trace, Time = DateTime.UtcNow, Message = msg });
	}

	public void Warn(String msg) {
		_logEntries.Add(new LogEntry { Severity = Severity.Warn, Time = DateTime.UtcNow, Message = msg });
	}

	public void Error(String msg) {
		_logEntries.Add(new LogEntry { Severity = Severity.Error, Time = DateTime.UtcNow, Message = msg });
	}

	#region Overrides of Object

	/// <inheritdoc />
	public override String ToString() => String.Join(Environment.NewLine, Entries);

	#endregion
}