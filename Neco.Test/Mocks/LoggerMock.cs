namespace Neco.Test.Mocks;

using System;
using Microsoft.Extensions.Logging;

public class LoggerMock<T> : ILogger<T> {
	public Int32 NumberOfLogCalls { get; set; }
	private readonly ILogger<T> _realLogger;

	// ReSharper disable once ContextualLoggerProblem
	public LoggerMock(ILogger<T> realLogger) {
		_realLogger = realLogger;
	}

	#region Implementation of ILogger

	/// <inheritdoc />
	public IDisposable BeginScope<TState>(TState state) {
		return _realLogger.BeginScope(state);
	}

	/// <inheritdoc />
	public Boolean IsEnabled(LogLevel logLevel) => true;

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, String> formatter) {
		++NumberOfLogCalls;
		_realLogger.Log(logLevel, eventId, state, exception, formatter);
	}

	#endregion
}