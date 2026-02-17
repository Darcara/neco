namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

// Zstd was evaluated with ZstdSharp.Port but produced consistently larger files than brotli
#if NET11_0_OR_GREATER
#error re-evaluate .Net 11 framework provided zstd compression
#endif
public enum CompressionMethod {
	None,
	Brotli,
	Gzip,
}