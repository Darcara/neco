namespace Neco.Common.Data.Auth;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;

/// <summary>
/// This implementation uses PBKDF2 also known as <see cref="Rfc2898DeriveBytes"/> to hash and verify passwords. 
/// </summary>
// PBKDF2 with HMAC-SHA512, 128-bit salt, 256-bit subkey, 250_000 iterations.
// Format: 1 Byte saltLength - 4 Byte little endian Int32 iterations - x Byte salt - 32 Byte hash.
public sealed class Pbkdf2Hasher : IPasswordHashingFunction {
	private const Byte _saltSize = 16;
	private const Byte _pepperSize = 16;
	private const Int32 _hashSize = 32;
	private const Int32 _iterations = 250_000;

	public static readonly Pbkdf2Hasher Instance = new();

	// Using this is almost the same as not using pepper and therefore has only marginal security gains 
	private static readonly Byte[] _defaultPepper = [0x9D, 0x96, 0x2E, 0x61, 0xAD, 0x03, 0x44, 0xB7, 0x07, 0x6F, 0x1B, 0xA1, 0xF4, 0xC8, 0xAC, 0x6F];
	private readonly Byte[] _pepper;

	public Pbkdf2Hasher() {
		_pepper = _defaultPepper;
	}

	/// <summary>
	/// Creates a new instance with a custom pepper value
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">If the pepper is not of the expected size (16)</exception>
	/// <remarks>The pepper should be stored in TPM or a similar secure facility</remarks>
	public Pbkdf2Hasher(Byte[] pepper) {
		if (pepper.Length != _pepperSize) throw new ArgumentOutOfRangeException(nameof(pepper), pepper.Length, $"Expected pepper to be of length {_pepperSize}, but was {pepper.Length}");
		_pepper = pepper;
	}

	#region Implementation of IPasswordHashingFunction

	/// <inheritdoc />
	public String Id => "PBKDF2";

	/// <inheritdoc />
	public String HashPassword(String username, String password) {
		Byte[] outputHash = new Byte[1 + 4 + _saltSize + _hashSize];
		outputHash[0] = _saltSize;
		BinaryPrimitives.WriteInt32LittleEndian(new Span<Byte>(outputHash, 1, 4), _iterations);
		
		Span<Byte> salt = new Byte[_saltSize + _pepperSize];
		RandomNumberGenerator.Fill(salt.Slice(0, _saltSize));
		_pepper.CopyTo(salt.Slice(_saltSize));

		Rfc2898DeriveBytes.Pbkdf2(password, salt, new Span<Byte>(outputHash, 1 + 4 + _saltSize, _hashSize), _iterations, HashAlgorithmName.SHA512);
		salt.Slice(0, _saltSize).CopyTo(new Span<Byte>(outputHash, 1 + 4, _saltSize));

		CryptographicOperations.ZeroMemory(salt);
		return Convert.ToBase64String(outputHash);
	}

	/// <inheritdoc />
	public Boolean VerifyPassword(String username, String password, String passwordHash) {
		Byte[] hashArray = ArrayPool<Byte>.Shared.Rent(1 + 4 + _saltSize + _hashSize);
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

		Span<Byte> salt = new Byte[saltSize + _pepperSize];
		new ReadOnlySpan<Byte>(hashArray, 1 + 4, saltSize).CopyTo(salt);
		_pepper.CopyTo(salt.Slice(saltSize));

		Byte[] passwordHashValidation = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, _hashSize);
		Boolean result = CryptographicOperations.FixedTimeEquals(passwordHashValidation, new ReadOnlySpan<Byte>(hashArray, 1 + 4 + saltSize, _hashSize));
		ArrayPool<Byte>.Shared.Return(hashArray);

		CryptographicOperations.ZeroMemory(salt);
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