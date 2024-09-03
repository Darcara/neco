namespace Neco.Common.Data.Archive;

using System;
using System.IO;

public sealed class EnumeratedFile : IEquatable<EnumeratedFile> {
	public readonly String NameInCatalog;
	public Stream DataStream => _streamGenerator();

	private readonly Func<Stream> _streamGenerator;

	public EnumeratedFile(String nameInCatalog, Func<Stream> streamGenerator) {
		NameInCatalog = nameInCatalog;
		_streamGenerator = streamGenerator;
	}

	#region Overrides of Object

	/// <inheritdoc />
	public override String ToString() => NameInCatalog;

	#endregion

	#region Equality members

	/// <inheritdoc />
	public Boolean Equals(EnumeratedFile? other) {
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return String.Equals(NameInCatalog,other.NameInCatalog, StringComparison.Ordinal);
	}

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => ReferenceEquals(this, obj) || obj is EnumeratedFile other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() => NameInCatalog.GetHashCode();

	public static Boolean operator ==(EnumeratedFile? left, EnumeratedFile? right) => Equals(left, right);

	public static Boolean operator !=(EnumeratedFile? left, EnumeratedFile? right) => !Equals(left, right);

	#endregion
}