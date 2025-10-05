namespace Neco.Common.Extensions;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

/// Extensions for ILogger
public static partial class ILoggerExtensions {
	/// <typeparam name="T">Any Exception that has a .ctor(String) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogCriticalAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Critical, null, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String, Exception) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogCriticalAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, Exception e, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Critical, e, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogErrorAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Error, null, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String, Exception) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogErrorAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, Exception e, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Error, e, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogWarningAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Warning, null, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String, Exception) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogWarningAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, Exception e, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Warning, e, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogDebugAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Debug, null, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String, Exception) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogDebugAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, Exception e, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Debug, e, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogTraceAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Trace, null, message);
	}

	/// <typeparam name="T">Any Exception that has a .ctor(String, Exception) - If the ctor is missing an <see cref="InvalidOperationException"/> will be thrown instead</typeparam>
	[DoesNotReturn]
	public static void LogTraceAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ILogger logger, Exception e, String message) where T : Exception {
		LogAndThrow<T>(logger, LogLevel.Trace, e, message);
	}

	[DoesNotReturn]
	private static void LogAndThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ILogger logger, LogLevel level, Exception? e, String message) {
		logger.Log(level, 0, e, message);
		if (e == null)
			throw (Exception?)Activator.CreateInstance(typeof(T), message) ?? new InvalidOperationException(message);
		throw (Exception?)Activator.CreateInstance(typeof(T), message, e) ?? new InvalidOperationException(message, e);
	}

}