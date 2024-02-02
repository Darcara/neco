namespace Neco.Common.Data;

using System;

/// <summary>
/// Whether we know if a file can be compressed, or not
/// </summary>
public enum FileCompression {
	/// The <see cref="IFileCompressionLookup"/>-instance does not know if this particular file extension can be compressed
	Unknown = 0,
	/// Compression of this file type will not result in a size reduction, or the compression is negligible
	Incompressible,
	/// Compression of this file type will result in a size reduction
	Compressible,
}

/// <summary>
/// A very simple lookup, whether a file can be compressed or not
/// </summary>
public interface IFileCompressionLookup {
	/// <summary>
	/// Returns whether the file extension is thought to be compressible or not
	/// </summary>
	/// <param name="fileExtension">The file extension to check. A starting dot '.' is optional</param>
	/// <param name="assumedDefault">What should be returned if compressability is unknown. Defaults to <see cref="FileCompression.Compressible"/></param>
	public FileCompression DoesFileCompress(String fileExtension, FileCompression assumedDefault = FileCompression.Compressible);
}