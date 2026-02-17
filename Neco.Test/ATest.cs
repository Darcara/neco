namespace Neco.Test;

using Extensions.Logging.NUnit;
using Microsoft.Extensions.Logging;

public abstract class ATest {
	protected static readonly LoggerFactory LoggerFactory = new([new NUnitLoggerProvider()]);
	protected static ILogger<T> GetLogger<T>() => LoggerFactory.CreateLogger<T>();

	protected readonly ILogger Logger;

	protected ATest() {
		Logger = LoggerFactory.CreateLogger(GetType());
	}
}