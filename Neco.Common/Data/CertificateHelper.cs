namespace Neco.Common.Data;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public static class CertificateHelper {
	public static X509Certificate2 CreateExportableSelfSignedSslCertificate(String cn, IPAddress? ip = null, X509Certificate2? rootCertificate = null, DateTime? validUntil = null) {
		using RSA key = RSA.Create();
		CertificateRequest request = new($"CN={cn}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

		// using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
		// CertificateRequest request = new("CN=" + cn, key, HashAlgorithmName.SHA256);
		request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
		request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
		request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
		request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection() {
			new("1.3.6.1.5.5.7.3.1"),
			new("1.3.6.1.5.5.7.3.2"),
		}, false));
		SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new();
		subjectAlternativeNameBuilder.AddDnsName(cn);
		if (ip != null)
			subjectAlternativeNameBuilder.AddIpAddress(ip);
		request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());

		if (rootCertificate == null) {
			// TODO .Create(...) for proper issuer
			return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), validUntil ?? DateTimeOffset.UtcNow.AddYears(5));
			// using X509Certificate2 sslcert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), validUntil ?? DateTimeOffset.UtcNow.AddYears(5));
			// return new X509Certificate2(sslcert.Export(X509ContentType.Pkcs12));
		}

		X509KeyUsageExtension? keyUsage = rootCertificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
		if (keyUsage == null || !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.KeyCertSign))
			throw new InvalidOperationException($"KeyUsage is not set or incompatible for {rootCertificate.SubjectName.Name}. Required is {X509KeyUsageFlags.KeyCertSign} -- Currently: '{keyUsage?.KeyUsages}'");
		Span<Byte> serialNumber = stackalloc Byte[8];
		RandomNumberGenerator.Fill(serialNumber);
		using X509Certificate2 sslcertPublic = request.Create(rootCertificate, DateTimeOffset.UtcNow.AddDays(-1), validUntil ?? rootCertificate.NotAfter, serialNumber);
		X509Certificate2 sslcert = sslcertPublic.CopyWithPrivateKey(key);
		return sslcert;
		// using X509Certificate2 sslcert = sslcertPublic.CopyWithPrivateKey(key);
		// return new X509Certificate2(sslcertPublic.Export(X509ContentType.Pkcs12));
	}

	public static X509Certificate2 CreateExportableSelfSignedRootCertificate(String cn, IPAddress? ip = null, DateTime? validUntil = null) {
		using RSA key = RSA.Create();
		CertificateRequest request = new($"CN={cn}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		
		// using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
		// CertificateRequest request = new("CN=" + cn, key, HashAlgorithmName.SHA256);


		request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
		request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
		request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.NonRepudiation, false));

		SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new();
		subjectAlternativeNameBuilder.AddDnsName(cn);
		if (ip != null)
			subjectAlternativeNameBuilder.AddIpAddress(ip);
		request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());
		
		return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), validUntil ?? DateTimeOffset.UtcNow.AddYears(5));
	}

	public static void CreateSelfSignedRootCertificateToFile(String saveTo, String cn, IPAddress? ip = null, DateTime? validUntil = null) {
		using var rootCert = CreateExportableSelfSignedRootCertificate(cn, ip, validUntil);
		// export
		SaveCert(rootCert, saveTo, true);

		String publicKeyName = Path.ChangeExtension(saveTo, "public" + Path.GetExtension(saveTo));
		SaveCert(rootCert, publicKeyName, false);
	}

	public static void SaveCert(X509Certificate2 cert, String file, Boolean includePrivateKey) {
		// export
		Char[] rawCert = PemEncoding.Write("CERTIFICATE", cert.RawData);
		Byte[] rawCertBytes = Encoding.UTF8.GetBytes(rawCert);

		AsymmetricAlgorithm? key = (AsymmetricAlgorithm?)cert.GetRSAPrivateKey() ?? cert.GetECDsaPrivateKey();
		Byte[] privateKeyBytes = key.ExportPkcs8PrivateKey();
		Char[] privateKeyPem = PemEncoding.Write("PRIVATE KEY", privateKeyBytes);
		Byte[] privateKeyPemBytes = Encoding.UTF8.GetBytes(privateKeyPem);

		using FileStream certFs = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.None);
		certFs.Write(rawCertBytes);
		if (includePrivateKey) {
			certFs.WriteByte(10);
			certFs.WriteByte(10);
			certFs.Write(privateKeyPemBytes);
		}
	}
	
	/// <summary>
	/// Required to use for ASP.NET and SslStream
	/// </summary>
	public static X509Certificate2 ConvertExportableCertToPkcs12(X509Certificate2 exportableCert) {
		return new X509Certificate2(exportableCert.Export(X509ContentType.Pkcs12));
	}

	/// <summary>
	/// Load a cert from disk. Not properly usable unless converted to Pkcs12
	/// </summary>
	public static X509Certificate2 LoadCertWithPrivateKey(String filename) {
		return X509Certificate2.CreateFromPemFile(filename);
	}

	/// <summary>
	/// Required to use for ASP.NET and SslStream
	/// </summary>
	public static X509Certificate2 LoadCertWithPrivateKeyPkcs12(String filename) {
		return new X509Certificate2(LoadCertWithPrivateKey(filename).Export(X509ContentType.Pkcs12));
	}
	
	public static X509Certificate2 LoadCertWithOnlyPublicKey(String filename) {
		return X509Certificate2.CreateFromPem(File.ReadAllText(filename));
	}
}