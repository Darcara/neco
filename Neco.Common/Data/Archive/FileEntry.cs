namespace Neco.Common.Data.Archive;

using System;
using Neco.Common.Extensions;

public readonly struct FileEntry : IComparable<FileEntry>, IComparable {
	/// <summary>
	/// The full name including the relative or absolute path
	/// </summary>
	public readonly String Name;

	/// <summary>
	/// Offset into the binary file where the file data starts
	/// </summary>
	public readonly Int64 Offset;

	/// <summary>
	/// Length of the data
	/// </summary>
	public readonly Int64 Length;

	public FileEntry(String name, Int64 offset, Int64 length) {
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(length);

		Name = name;
		Offset = offset;
		Length = length;
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