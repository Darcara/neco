namespace Neco.Common.Data.Auth;

using System;

/// <summary>
/// Authenticates a single inMemory user
/// </summary>
public class SingleUser : IAuthenticationProvider {
	private readonly String _username;
	private readonly String _passwordHash;

	public SingleUser(String username, String password) {
		ArgumentNullException.ThrowIfNull(username);
		ArgumentNullException.ThrowIfNull(password);
		
		_username = username;
		_passwordHash = Pbkdf2Hasher.Instance.HashPassword(username, password);
	}

	#region Implementation of IAuthenticationProvider

	/// <inheritdoc />
	public AuthResult CheckAuth(String username, String password) {
		ArgumentNullException.ThrowIfNull(username);
		ArgumentNullException.ThrowIfNull(password);
		
		if (!String.Equals(username, _username, StringComparison.Ordinal))
			return AuthResult.Failed;
		
		if (!Pbkdf2Hasher.Instance.VerifyPassword(username, password, _passwordHash))
			return AuthResult.Failed;

		return AuthResult.Authenticated;
	}

	#endregion
}