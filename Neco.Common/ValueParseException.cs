namespace Neco.Common;

/// <summary>
/// The exception that is thrown when a value cannot be parsed to the expected type.
/// </summary>
/// <remarks>
/// This exception is used throughout the Neco.Common library to indicate parsing failures
/// when converting string or other values to strongly-typed objects.
/// </remarks>
public class ValueParseException : Exception {
	/// <summary>
	/// Initializes a new instance of the <see cref="ValueParseException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the parsing error.</param>
	public ValueParseException(String message)
		: base(message) {
	}
}