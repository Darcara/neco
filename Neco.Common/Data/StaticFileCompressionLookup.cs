namespace Neco.Common.Data;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

/// <summary>
/// A static list of incompressible files. Compressible files will return the assumedDefault, which should be <see cref="FileCompression.Compressible"/>.
/// </summary>
public sealed class StaticFileCompressionLookup : IFileCompressionLookup {
	public static readonly StaticFileCompressionLookup Instance = new();

	private static readonly FrozenSet<String> _incompressibleExtensions = new HashSet<String>(StringComparer.OrdinalIgnoreCase) {
		"3G2", "3GP", "7Z", "AA3", "AAC", "ACE", "AES", "AIF", "ALZ", "APE", "APK", "APNG", "APPX", "ARJ", "ASF",
		"ASX", "AVI", "AXX", "BIK", "BR", "BSF", "BZ", "BZ2", "CAB", "CBR", "CBZ", "CHM", "CR2", "CSVZ", "D", "DEB",
		"DESS", "DIVX", "DL_", "DMG", "DNG", "DOCM", "DOCX", "DOTM", "DOTX", "DSFT", "EFTX", "EMZ",
		"EOT", "EPUB", "EX_", "F4V", "FLAC", "FLV", "FVE", "GIF", "GPG", "GRAFFLE", "GSM", "GZ", "GZ2", "HC",
		"HDMOV", "HXS", "I", "IDX", "IFF", "J2C", "JAR", "JFIF", "JPEG", "JPG", "KDBX", "LZMA", "LZ", "LZ4",
		"LZO", "M2P", "M3P", "M4A", "M4V", "MAX", "MCACHE", "MKV", "MOBI", "MOV", "MP3", "MP4", "MPA", "MPC",
		"MPEG", "MPG", "MPKG", "MPQ", "MSHC", "MSI", "MSIX", "MSP", "MSU", "MTS", "NEF", "NUPKG", "ODP", "ODS",
		"ODT", "OFR", "OFS", "OGG", "OGM", "OGV", "OPUS", "OTP", "OTS", "OTT", "PACK", "PAGES", "PAMP",
		"PDN", "PET", "PNG", "PPTX", "PSPIMAGE", "QSM", "RA", "RAR", "RM", "RPM", "S7Z", "SDG", "SFT", "SNUPKG",
		"SIT", "SITX", "STW", "SWF", "SWZ", "SY_", "TBZ2", "TC", "TGZ", "THMX", "TIB", "TIF", "TIFF", "TORRENT",
		"TPM", "TRF", "TRP", "TS", "TXZ", "UNITYPACKAGE", "VDB", "VHD", "VOB", "VSIX", "VSV", "WAR", "WEBARCHIVE",
		"WEBM", "WEBP", "WHL", "WIM", "WMA", "WMV", "WMZ", "WOFF", "WOFF2", "WTV", "WV", "XAR", "XLSB", "XLSM",
		"XLSX", "XMF", "XPI", "XPS", "XZ", "Z", "ZIP", "ZIPX", "ZST",
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenSet<String>.AlternateLookup<ReadOnlySpan<Char>> _incompressibleExtensionsLookup = _incompressibleExtensions.GetAlternateLookup<ReadOnlySpan<Char>>();

	internal static ReadOnlySpan<Char> NormalizeFileExtension(ReadOnlySpan<Char> fileExtension) {
		if (fileExtension.Length == 0) return String.Empty;
		if (fileExtension[0] == '.') return fileExtension.Slice(1);
		return fileExtension;
	}

	/// <inheritdoc />
	public FileCompression DoesFileCompress(ReadOnlySpan<Char> fileExtension, FileCompression assumedDefault = FileCompression.Compressible) {
		if (fileExtension.Length == 0) return assumedDefault;
		ReadOnlySpan<Char> extensionStr = NormalizeFileExtension(fileExtension);
		if (extensionStr.Length == 0) return assumedDefault;
		return _incompressibleExtensionsLookup.Contains(extensionStr) ? FileCompression.Incompressible : assumedDefault;
	}
}