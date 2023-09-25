namespace Neco.Common;

using System;

public class ValueParseException : Exception {
	/// <summary>
	/// Initializes a new instance of the <see cref="T:System.Exception"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error. </param>
	public ValueParseException(String message)
		: base(message) {
	}
}