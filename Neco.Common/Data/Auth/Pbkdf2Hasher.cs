namespace Neco.Common.Data.Auth;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;

/// <summary>
/// This implementation uses PBKDF2 also known as <see cref="Rfc2898DeriveBytes"/> to hash and verify passwords. 
/// </summary>
// PBKDF2 with HMAC-SHA512, 128-bit salt, 256-bit subkey, 100_000 iterations.
// Format: 1 Byte saltLength - 4 Byte little endian Int32 iterations - x Byte salt - 32 Byte hash.
public sealed class Pbkdf2Hasher : IPasswordHashingFunction {
	private const Byte _saltSize = 16;
	private const Int32 _hashSize = 32;
	private const Int32 _iterations = 100_000;
	
	public static readonly Pbkdf2Hasher Instance = new ();

	#region Implementation of IPasswordHashingFunction

	/// <inheritdoc />
	public String Id => "PBKDF2";

	/// <inheritdoc />
	public String HashPassword(String username, String password) {
		Byte[] outputHash = new Byte[1 + 4 + _saltSize + _hashSize];
		outputHash[0] = _saltSize;
		BinaryPrimitives.WriteInt32LittleEndian(new Span<Byte>(outputHash, 1, 4), _iterations);

		Span<Byte> salt = new(outputHash, 1 + 4, _saltSize);
		RandomNumberGenerator.Fill(salt);

		Rfc2898DeriveBytes.Pbkdf2(password, salt, new Span<Byte>(outputHash, 1 + 4 + _saltSize, _hashSize), _iterations, HashAlgorithmName.SHA512);

		return Convert.ToBase64String(outputHash);
	}

	/// <inheritdoc />
	public Boolean VerifyPassword(String username, String password, String passwordHash) {
		Byte[] hashArray = ArrayPool<Byte>.Shared.Rent(1024);
		if (!Convert.TryFromBase64String(passwordHash, hashArray, out Int32 bytesWritten) || bytesWritten < 1 + 4 + _hashSize) {
			ArrayPool<Byte>.Shared.Return(hashArray);
			return false;
		}

		Byte saltSize = hashArray[0];
		if (bytesWritten < 1 + 4 + _hashSize + saltSize)
			return false;
		Int32 iterations = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<Byte>(hashArray, 1, 4));
		if (iterations <= 0)
			return false;
		ReadOnlySpan<Byte> salt = new(hashArray, 1 + 4, saltSize);

		Byte[] passwordHashValidation = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, _hashSize);
		Boolean result = passwordHashValidation.SequenceEqual(hashArray.Skip(1 + 4 + saltSize).Take(_hashSize));
		ArrayPool<Byte>.Shared.Return(hashArray);
		return result;
	}

	#endregion

	#region Overrides of Object

	/// <inheritdoc />
	public override String ToString() => Id;

	#region Equality members

	private Boolean Equals(Pbkdf2Hasher other) => Id.Equals(other.Id, StringComparison.Ordinal);

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => ReferenceEquals(this, obj) || obj is Pbkdf2Hasher other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() => Id.GetHashCode();

	public static Boolean operator ==(Pbkdf2Hasher? left, Pbkdf2Hasher? right) => Equals(left, right);

	public static Boolean operator !=(Pbkdf2Hasher? left, Pbkdf2Hasher? right) => !Equals(left, right);

	#endregion

	#endregion
}