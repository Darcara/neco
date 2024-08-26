namespace Neco.Common.Data.Archive;

using System;
using Neco.Common.Data.Hash;
using Neco.Common.Extensions;

public readonly struct FileEntry : IEquatable<FileEntry>, IComparable<FileEntry>, IComparable {
	/// <summary>
	/// The full name including the relative or absolute path
	/// </summary>
	public readonly String Name;

	/// <summary>
	/// Offset into the binary file where the file data starts
	/// </summary>
	public readonly Int64 Offset;

	/// <summary>
	/// Length of the data in the binary file
	/// </summary>
	public readonly Int64 Length;

	/// <summary>
	/// TRUE if the data is compressed
	/// </summary>
	/// /// <seealso cref="CatalogFeatures.CompressionPerEntryOptimal"/>
	/// /// <seealso cref="CatalogFeatures.CompressionPerEntrySmallest"/>
	public readonly Boolean IsCompressed;

	/// <summary>
	/// Length of the uncompressed data, or zero(0) if not available. If the data is not compressed this is the same as <see cref="Length"/>
	/// </summary>
	/// /// <seealso cref="CatalogFeatures.UncompressedFileSize"/>
	public readonly Int64 UncompressedLength;

	/// <summary>
	/// A <see cref="WyHashFinal3"/> hash of the uncompressed data. Zero(0) if no hash is available.
	/// </summary>
	/// <seealso cref="CatalogFeatures.ChecksumPerEntry"/>
	public readonly Int64 Checksum;

	public FileEntry(String name, Int64 offset, Int64 length, Boolean isCompressed, Int64 uncompressedLength, Int64 checksum) {
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(length);

		Name = name;
		Offset = offset;
		Length = length;
		IsCompressed = isCompressed;
		UncompressedLength = isCompressed ? uncompressedLength : Length;
		Checksum = checksum;
	}

	internal FileEntry(FileEntry baseEntry, Int64 length) {
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		Name = baseEntry.Name;
		Offset = baseEntry.Offset;
		Length = length;
		IsCompressed = baseEntry.IsCompressed;
		UncompressedLength = baseEntry.IsCompressed ? baseEntry.UncompressedLength : length;
		Checksum = baseEntry.Checksum;
	}

	#region Relational members

	/// <inheritdoc />
	public Int32 CompareTo(FileEntry other) => Offset.CompareTo(other.Offset);

	/// <inheritdoc />
	public Int32 CompareTo(Object? obj) {
		if (ReferenceEquals(null, obj)) return 1;
		return obj is FileEntry other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(FileEntry)}");
	}

	public static Boolean operator <(FileEntry left, FileEntry right) => left.CompareTo(right) < 0;

	public static Boolean operator >(FileEntry left, FileEntry right) => left.CompareTo(right) > 0;

	public static Boolean operator <=(FileEntry left, FileEntry right) => left.CompareTo(right) <= 0;

	public static Boolean operator >=(FileEntry left, FileEntry right) => left.CompareTo(right) >= 0;

	#endregion

	#region Equality members

	public Boolean Equals(FileEntry other) => Name == other.Name && Offset == other.Offset && Length == other.Length;

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => obj is FileEntry other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() => Offset.GetHashCode();

	public static Boolean operator ==(FileEntry left, FileEntry right) => left.Equals(right);

	public static Boolean operator !=(FileEntry left, FileEntry right) => !left.Equals(right);

	#endregion

	#region Overrides of ValueType

	/// <inheritdoc />
	public override String ToString() => $"{Name} {Length.ToFileSize()} @{Offset:X8}";

	#endregion
}