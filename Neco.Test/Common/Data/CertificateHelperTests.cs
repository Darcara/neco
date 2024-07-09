namespace Neco.Test.Common.Data;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Neco.Common.Data;
using NUnit.Framework;

[TestFixture]
public class CertificateHelperTests {
	[SetUp]
	public void BeforeEachTest() {
		File.Delete("./ssl.crt");
		File.Delete("./root.crt");
		File.Delete("./root.public.crt");
		File.Delete("./some.crt");
		File.Delete("./some.public.crt");
	}
	[Test]
	public void GeneratesSslCertificateCorrectly() {
		X509Certificate2 originalCert = CertificateHelper.CreateExportableSelfSignedSslCertificate("localhost", IPAddress.Parse("127.0.0.1"));
		CertificateHelper.SaveCert(originalCert, "./ssl.crt", true);
		Assert.That(File.Exists("./ssl.crt"));

		using X509Certificate2 cert = CertificateHelper.LoadCertWithPrivateKey("./ssl.crt");
		Assert.That(cert.HasPrivateKey);
	}

	[Test]
	public void GeneratesRootCertificateCorrectly() {
		CertificateHelper.CreateSelfSignedRootCertificateToFile("./root.crt", "localhost");
		Assert.That(File.Exists("./root.crt"));
		Assert.That(File.Exists("./root.public.crt"));

		using X509Certificate2 certPrivate = CertificateHelper.LoadCertWithPrivateKey("./root.crt");
		using X509Certificate2 certPublic = CertificateHelper.LoadCertWithOnlyPublicKey("./root.public.crt");
		Assert.That(certPrivate.HasPrivateKey);
		Assert.That(certPublic.HasPrivateKey, Is.False);
		Assert.That(certPublic.Thumbprint.SequenceEqual(certPrivate.Thumbprint));
	}
	
	[Test]
	public void LoadingTheWrongTypeFails() {
		CertificateHelper.CreateSelfSignedRootCertificateToFile("./some.crt", "localhost");
		Assert.That(File.Exists("./some.crt"));
		Assert.That(File.Exists("./some.public.crt"));

		using X509Certificate2 certPrivateAsPublic = CertificateHelper.LoadCertWithOnlyPublicKey("./some.crt");
		Assert.That(certPrivateAsPublic.HasPrivateKey, Is.False);

		Assert.Throws<CryptographicException>(() => {
			using X509Certificate2 certPublic = CertificateHelper.LoadCertWithPrivateKey("./some.public.crt");
		});
	}

	private void LoadAndValidate(String thumbprint, String privateKeyFile, String? publicKeyOnlyFile) {
		// load and validate
		using X509Certificate2 loadedCertWithPrivateKey = X509Certificate2.CreateFromPemFile(privateKeyFile);
		Assert.That(loadedCertWithPrivateKey.Thumbprint.SequenceEqual(thumbprint) && loadedCertWithPrivateKey.HasPrivateKey, "Cert with private key: thumbprint or private key mismatch");
		if (publicKeyOnlyFile != null) {
			using X509Certificate2 loadedCertPublicKeyOnly = X509Certificate2.CreateFromPem(File.ReadAllText(publicKeyOnlyFile));
			Assert.That(loadedCertPublicKeyOnly.Thumbprint.SequenceEqual(thumbprint) && !loadedCertPublicKeyOnly.HasPrivateKey, "Cert with public key only: thumbprint mismatch or private key present");
		}
	}
}