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

	internal static String NormalizeFileExtension(String fileExtension) {
#if NET9_0_OR_GREATER
		// TODO: we can work with ReadOnlySpans (probably NET9) https://github.com/dotnet/runtime/issues/27229
		#error Check ReadOnlySpanLookup in https://github.com/dotnet/runtime/issues/27229
#endif

		if (fileExtension.Length == 0) return String.Empty;
		String extensionStr = fileExtension;
		if (extensionStr[0] == '.') extensionStr = extensionStr.Substring(1);

		return extensionStr;
	}

	/// <inheritdoc />
	public FileCompression DoesFileCompress(String fileExtension, FileCompression assumedDefault = FileCompression.Compressible) {
		ArgumentNullException.ThrowIfNull(fileExtension);
		String extensionStr = NormalizeFileExtension(fileExtension);
		if (extensionStr.Length == 0) return assumedDefault;
		return _incompressibleExtensions.Contains(extensionStr) ? FileCompression.Incompressible : assumedDefault;
	}
}