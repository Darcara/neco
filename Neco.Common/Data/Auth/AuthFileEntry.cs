namespace Neco.Common.Data.Auth;

using System;

internal class AuthFileEntry {
	public String HashFunc { get; }
	public Int32 AuthLevel { get; }
	public String HashedPassword { get; }
	public String Username { get; }

	public AuthFileEntry(String hashFunc, Int32 authLevel, String hashedPassword, String username) {
		HashFunc = hashFunc;
		AuthLevel = authLevel;
		HashedPassword = hashedPassword;
		Username = username;
	}

	public String Serialize() => $"{HashFunc}{AuthFile.PartSeparator}{AuthLevel}{AuthFile.PartSeparator}{HashedPassword}{AuthFile.PartSeparator}{Username}";

	public static AuthFileEntry DeSerialize(String authFileEntry) {
		ArgumentException.ThrowIfNullOrEmpty(authFileEntry);

		String[] parts = authFileEntry.Split('$');
		if (parts.Length != 4)
			throw new ArgumentException("invalid entry", nameof(authFileEntry));

		try {
			return new AuthFileEntry(parts[0], Int32.Parse(parts[1]), parts[2],parts[3]);
		}
		catch (Exception e) {
			throw new ArgumentException("invalid entry", nameof(authFileEntry), e);
		}
	}

	internal AuthFileEntry WithAuthLevel(Int32 authLevel) => new(HashFunc, authLevel, HashedPassword, Username);
}