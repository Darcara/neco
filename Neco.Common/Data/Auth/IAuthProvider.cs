namespace Neco.Common.Data.Auth;

using System;

public interface IAuthenticationProvider {
	/// <summary>
	/// <para>Check if the supplied credentials are correct</para>
	/// </summary>
	/// <param name="username">The username to check</param>
	/// <param name="password">The plaintext passowrd as entered by the user</param>
	/// <returns><see cref="AuthResult.Failed"/> if the user is not found, the hasher is unkonwn or the hashed password did not match; true otherwise</returns>
	/// <exception cref="ArgumentException">If <see cref="username"/> is null or empty</exception>
	/// <exception cref="ArgumentNullException">If <see cref="password"/> is null</exception>
	public AuthResult CheckAuth(String username, String password);
}