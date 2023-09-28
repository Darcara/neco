namespace Neco.Common.Data.Auth;

using System;

/// <summary>
/// Implementations of this interface are responsible to hash plain text passwords and verify hashed passwords.
/// </summary>
public interface IPasswordHashingFunction {
	/// A unique name this <see cref="IPasswordHashingFunction"/> is identified by
	public String Id { get; }
	/// Hashes the plain text user input into an opaque string
	public String HashPassword(String username, String password);

	/// Verifies, that the opaque string from <see cref="HashPassword"/> is the same as the given plain text password
	/// <exception cref="FormatException">When the <see cref="passwordHash"/> is not a valid base64 encoded string</exception>
	public Boolean VerifyPassword(String username, String password, String passwordHash);
}