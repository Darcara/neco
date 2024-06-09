namespace Neco.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

internal static class Helper {
	public static void CompressFile(String inputName, String outputName) {
		Byte[] readAllBytes = File.ReadAllBytes("./TestData/" + inputName);

		using FileStream fileOut = File.OpenWrite("./TestData/" + outputName);
		using BrotliStream outStream = new(fileOut, CompressionLevel.SmallestSize);
		outStream.Write(readAllBytes);
	}

	public static Byte[] ReadCompressedFile(String name) {
		String extension = Path.GetExtension(name);
		using FileStream fileIn = File.OpenRead("./TestData/" + name);
		Stream inStream = extension switch {
			".br" => new BrotliStream(fileIn, CompressionMode.Decompress),
			".gz" => new BrotliStream(fileIn, CompressionMode.Decompress),
			_ => fileIn,
		};

		using MemoryStream memStream = new();
		inStream.CopyTo(memStream);
		return memStream.ToArray();
	}

	public static String ReadCompressedFileAsString(String name, Encoding? enc = null) {
		enc ??= new UTF8Encoding(false);

		return enc.GetString(ReadCompressedFile(name));
	}
}