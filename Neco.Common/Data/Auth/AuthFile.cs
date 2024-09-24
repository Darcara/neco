namespace Neco.Common.Data.Auth;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Represents a file consisting of user/password combinations.
/// All user names and passwords are case sensitive.
/// </summary>
/// <remarks>This class is not thread safe</remarks>
public class AuthFile : IAuthenticationProvider{
	/// <summary>
	/// Characters that are illegal to use in username <see cref="IPasswordHashingFunction"/>.<see cref="IPasswordHashingFunction.Id"/> and 
	/// </summary>
	public static readonly HashSet<Char> IllegalChars = [PartSeparator, EntrySeparator, CommentFirstChar];

	internal const Char PartSeparator = '$';
	internal const Char EntrySeparator = '\n';
	internal const Char CommentFirstChar = '#';
	private static readonly Encoding _fileEncoding = new UTF8Encoding(false);
	private readonly String _filename;
	private readonly Dictionary<String, AuthFileEntry> _authFileEntries = new(StringComparer.Ordinal);
	private readonly Dictionary<String, IPasswordHashingFunction> _knownHashingFunctions = new(StringComparer.Ordinal);

	public AuthFile(String filename) {
		if (String.IsNullOrWhiteSpace(filename))
			throw new ArgumentNullException(nameof(filename));

		_filename = filename;
		AddHashingFunction(Pbkdf2Hasher.Instance);

		LoadFile();
	}

	/// <summary>
	/// Registers a new password hashing algorithm
	/// </summary>
	/// <exception cref="ArgumentNullException">If <see cref="passwordHashingFunction"/> is null</exception>
	/// <exception cref="ArgumentException">If <see cref="IPasswordHashingFunction.Id"/> contains any of <see cref="IllegalChars"/></exception>
	/// <exception cref="ArgumentException">If an element with the same <see cref="IPasswordHashingFunction.Id"/> already exists</exception>
	public void AddHashingFunction(IPasswordHashingFunction passwordHashingFunction) {
		ArgumentNullException.ThrowIfNull(passwordHashingFunction);
		if (String.IsNullOrEmpty(passwordHashingFunction.Id) || passwordHashingFunction.Id.Any(c => IllegalChars.Contains(c)))
			throw new ArgumentException("Illegal characters in password hashing function id", nameof(passwordHashingFunction));
		_knownHashingFunctions.Add(passwordHashingFunction.Id, passwordHashingFunction);
	}
	
	/// <summary>
	/// Removes all registeres hashing functions
	/// </summary>
	public void ClearAllHashingFunctions() {
		_knownHashingFunctions.Clear();
	}

	private void LoadFile() {
		_authFileEntries.Clear();
		if (!File.Exists(_filename))
			return;
		foreach (AuthFileEntry? entry in File.ReadAllLines(_filename, _fileEncoding).Where(line => !String.IsNullOrWhiteSpace(line) && !line.StartsWith(CommentFirstChar)).Select(AuthFileEntry.DeSerialize))
			_authFileEntries.Add(entry.Username, entry);
	}

	private void SaveFile() {
		if (_authFileEntries == null! || _authFileEntries.Count == 0) {
			File.WriteAllText(_filename, String.Empty, _fileEncoding);
			return;
		}

		using FileStream outStream = File.Open(_filename, FileMode.Create, FileAccess.Write, FileShare.None);
		foreach (KeyValuePair<String, AuthFileEntry> entry in _authFileEntries) {
			outStream.Write(_fileEncoding.GetBytes(entry.Value.Serialize()));
			outStream.WriteByte((Byte)EntrySeparator);
		}
	}

	/// <summary>
	/// Determines whether the <see cref="AuthFile"/> contains the specified user.
	/// </summary>
	/// <returns>true if the <see cref="AuthFile"/> contains a user with the specified key; otherwise, false</returns>
	public Boolean UserExists(String username) => _authFileEntries.ContainsKey(username);

	/// <summary>
	/// Removes the value with the specified username
	/// </summary>
	/// <param name="username">The user to remove</param>
	/// <returns>true if the element is successfully found and removed; otherwise, false. This method returns false if key is not found</returns>
	public Boolean DeleteUser(String username) {
		Boolean hasBeenDeleted = _authFileEntries.Remove(username);

		if (hasBeenDeleted)
			SaveFile();
		return hasBeenDeleted;
	}

	/// <summary>
	/// Adds a new user to the <see cref="AuthFile"/> 
	/// </summary>
	/// <param name="username">The name of the user to add, must be unique</param>
	/// <param name="password">The clear text password</param>
	/// <param name="pwHasher">The password hashing algorithm to use</param>
	/// <param name="authLevel">The <see cref="AuthFileEntry.AuthLevel"/> for the user</param>
	/// <exception cref="ArgumentException">If <see cref="username"/> already exist</exception>
	/// <exception cref="ArgumentException">If <see cref="username"/> is null or empty</exception>
	/// <exception cref="ArgumentException">If <see cref="username"/> contains any <see cref="IllegalChars"/></exception>
	/// <exception cref="ArgumentException">If <see cref="pwHasher"/>.Id contains any <see cref="IllegalChars"/></exception>
	/// <exception cref="ArgumentNullException">If <see cref="password"/> is null</exception>
	/// <exception cref="ArgumentNullException">If <see cref="pwHasher"/> is null</exception>
	public void AddUser(String username, String password, IPasswordHashingFunction pwHasher, Int32 authLevel = 1) {
		ArgumentException.ThrowIfNullOrEmpty(username);
		ArgumentNullException.ThrowIfNull(password);
		ArgumentNullException.ThrowIfNull(pwHasher);
		if (pwHasher.Id.Any(c => IllegalChars.Contains(c)))
			throw new ArgumentException("Illegal characters in passwoth hashing function id", nameof(pwHasher));
		if (username.Any(c => IllegalChars.Contains(c)))
			throw new ArgumentException("Illegal characters in passwoth hashing function id", nameof(username));

		if (_authFileEntries.ContainsKey(username))
			throw new ArgumentException("User already exists", nameof(username));

		AuthFileEntry entry = CreateNewAuthFileEntry(username, password, pwHasher, authLevel);

		_authFileEntries.Add(username, entry);

		SaveFile();
	}

	private static AuthFileEntry CreateNewAuthFileEntry(String username, String password, IPasswordHashingFunction pwHasher, Int32 authLevel) {
		Debug.Assert(!String.IsNullOrEmpty(username));
		Debug.Assert(password != null);
		Debug.Assert(pwHasher != null);

		String hashedPassword = pwHasher.HashPassword(username, password);

		return new AuthFileEntry(pwHasher.Id, authLevel, hashedPassword, username);
	}

	/// <summary>
	/// Changes the password and/or the password hashing algorithm for a user
	/// </summary>
	/// <param name="username">The user</param>
	/// <param name="password">The password to be used</param>
	/// <param name="pwHasher">The hashing algorithm to use</param>
	/// <exception cref="ArgumentException">If <see cref="username"/> does not exist</exception>
	/// <exception cref="ArgumentException">If <see cref="username"/> is null or empty</exception>
	/// <exception cref="ArgumentException">If <see cref="username"/> contains any <see cref="IllegalChars"/></exception>
	/// <exception cref="ArgumentException">If <see cref="pwHasher"/>.Id contains any <see cref="IllegalChars"/></exception>
	/// <exception cref="ArgumentNullException">If <see cref="password"/> is null</exception>
	/// <exception cref="ArgumentNullException">If <see cref="pwHasher"/> is null</exception>
	public void ChangePassword(String username, String password, IPasswordHashingFunction pwHasher) {
		ArgumentException.ThrowIfNullOrEmpty(username);
		ArgumentNullException.ThrowIfNull(password);
		ArgumentNullException.ThrowIfNull(pwHasher);
		if (pwHasher.Id.Any(c => IllegalChars.Contains(c)))
			throw new ArgumentException("Illegal characters in passwoth hashing function id", nameof(pwHasher));
		if (username.Any(c => IllegalChars.Contains(c)))
			throw new ArgumentException("Illegal characters in passwoth hashing function id", nameof(username));

		if (!_authFileEntries.TryGetValue(username, out AuthFileEntry? authFileEntry))
			throw new ArgumentException("User does not exist", nameof(username));

		AuthFileEntry entry = CreateNewAuthFileEntry(username, password, pwHasher, authFileEntry.AuthLevel);
		_authFileEntries[username] = entry;

		SaveFile();
	}

	/// <summary>
	/// Changes the <see cref="authLevel"/> of a user. Has no effect if the user is not known
	/// </summary>
	/// <param name="username">The user</param>
	/// <param name="authLevel">The new authLevel to use</param>
	/// <exception cref="ArgumentException">If <see cref="username"/> is null or empty</exception>
	public void ChangeUser(String username, Int32 authLevel) {
		ArgumentException.ThrowIfNullOrEmpty(username);
		if (!_authFileEntries.TryGetValue(username, out var authFileEntry))
			return;

		_authFileEntries[username] = authFileEntry.WithAuthLevel(authLevel);

		SaveFile();
	}

	/// <inheritdoc />
	public AuthResult CheckAuth(String username, String password) => CheckAuth(username, password, out Int32 _);

	/// <summary>
	/// <para>Check if the supplied credentials are correct</para>
	/// </summary>
	/// <param name="username">The username to check</param>
	/// <param name="password">The plaintext passowrd as entered by the user</param>
	/// <param name="authLevel">The authLevel of the <see cref="AuthResult.Authenticated"/> user; 0 if result is <see cref="AuthResult.Failed"/></param>
	/// <returns><see cref="AuthResult.Failed"/> if the user is not found, the hasher is unkonwn or the hashed password did not match; true otherwise</returns>
	/// <exception cref="ArgumentException">If <see cref="username"/> is null or empty</exception>
	/// <exception cref="ArgumentNullException">If <see cref="password"/> is null</exception>
	public AuthResult CheckAuth(String username, String password, out Int32 authLevel) {
		ArgumentException.ThrowIfNullOrEmpty(username);
		ArgumentNullException.ThrowIfNull(password);
		authLevel = 0;

		// User not found is not an additional error, just failed
		if (!_authFileEntries.TryGetValue(username, out AuthFileEntry? authFileEntry))
			return AuthResult.Failed;

		if (!_knownHashingFunctions.TryGetValue(authFileEntry.HashFunc, out IPasswordHashingFunction? pwHasher))
			return AuthResult.Failed;

		if (!pwHasher.VerifyPassword(username, password, authFileEntry.HashedPassword))
			return AuthResult.Failed;

		authLevel = authFileEntry.AuthLevel;
		return AuthResult.Authenticated;
	}
}