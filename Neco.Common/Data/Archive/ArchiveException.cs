namespace Neco.Common.Data.Archive;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

public sealed class ArchiveException : Exception {
	/// <inheritdoc />
	public ArchiveException() {
	}

	/// <inheritdoc />
	public ArchiveException(String? message) : base(message) {
	}

	/// <inheritdoc />
	public ArchiveException(String? message, Exception? innerException) : base(message, innerException) {
	}

	[DoesNotReturn]
	public static void Throw(String message) => throw new ArchiveException(message);
	
	public static void ThrowIfNegative<T>(T value, String? message = null, [CallerArgumentExpression(nameof(value))] String? paramName = null) where T : INumberBase<T> {
		if (T.IsNegative(value))
			ThrowNegative(value, message, paramName);
	}

	[DoesNotReturn]
	private static void ThrowNegative<T>(T value, String? message, String? paramName) => throw new ArchiveException(message ?? $"{paramName} cannot be negative, but was {value}");
}