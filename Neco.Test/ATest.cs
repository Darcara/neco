namespace Neco.Test;

using Extensions.Logging.NUnit;
using Microsoft.Extensions.Logging;

public class ATest {
	protected static readonly LoggerFactory LoggerFactory = new(new[] { new NUnitLoggerProvider() });
	protected static ILogger<T> GetLogger<T>() => LoggerFactory.CreateLogger<T>();
}