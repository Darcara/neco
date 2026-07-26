namespace Neco.Common.Processing.Threading;

/// <inheritdoc />
public class SchedulingExecutionException : Exception {
	public TimeSpan Retry { get; }

	/// <inheritdoc />
	public SchedulingExecutionException() : this(null) {
	}

	/// <inheritdoc />
	public SchedulingExecutionException(TimeSpan? retry = null) {
		Retry = retry ?? TimeSpan.MinValue;
	}

	/// <inheritdoc />
	public SchedulingExecutionException(String? message, TimeSpan? retry = null) : base(message) {
		Retry = retry ?? TimeSpan.MinValue;
	}

	/// <inheritdoc />
	public SchedulingExecutionException(String? message, Exception? innerException, TimeSpan? retry = null) : base(message, innerException) {
		Retry = retry ?? TimeSpan.MinValue;
	}
}