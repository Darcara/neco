namespace Neco.Test.Common.Data.Auth;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FluentAssertions;
using Neco.Common.Data.Auth;
using NUnit.Framework;

[TestFixture]
public class AuthFileTests {
	private const String _username = "SomeName";
	private const String _illegalUsername = "Illegal#Username";
	private const String _pw = "SomePassword";
	private const String _pwOther = "AnotherPassword";
	private const String _authFileName = "./testAuth";

	[SetUp]
	[TearDown]
	public void BeforeAndAfterTest() {
		if (File.Exists(_authFileName))
			File.Delete(_authFileName);
	}

	[Test]
	public void CanDeleteUser() {
		AuthFile authFile = new(_authFileName);
		authFile.AddUser(_username, _pw, Pbkdf2Hasher.Instance, 5);
		authFile.UserExists(_username).Should().BeTrue();
		authFile.DeleteUser(_username).Should().BeTrue();
		authFile.DeleteUser(_username).Should().BeFalse();
	}

	[Test]
	public void PersistsCorrectly() {
		{
			AuthFile authFile = new(_authFileName);
			authFile.AddUser(_username, _pw, Pbkdf2Hasher.Instance, 5);
			authFile.UserExists(_username).Should().BeTrue();
			authFile.CheckAuth(_username, _pw).Should().Be(AuthResult.Authenticated);
		}

		{
			AuthFile authFile2 = new(_authFileName);
			authFile2.UserExists(_username).Should().BeTrue();
			authFile2.CheckAuth(_username, _pw).Should().Be(AuthResult.Authenticated);
		}
	}

	[Test]
	public void IgnoresCommentsInFile() {
		PersistsCorrectly();
		String authData = File.ReadAllText(_authFileName);
		File.WriteAllText(_authFileName, "# CommentFirstLine \n" + authData + "\n\n# CommentAfter");
		{
			AuthFile authFile2 = new(_authFileName);
			authFile2.UserExists(_username).Should().BeTrue();
			authFile2.CheckAuth(_username, _pw).Should().Be(AuthResult.Authenticated);
		}
	}

	[Test]
	[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
	public void FailsOnInvalidEntriesInFile() {
		PersistsCorrectly();
		String authData = File.ReadAllText(_authFileName);
		// Space as first char
		File.WriteAllText(_authFileName, "# CommentFirstLine \n" + authData + "\n\n # CommentAfter");

		Assert.Throws<ArgumentException>(() => new AuthFile(_authFileName));

		BeforeAndAfterTest();
		PersistsCorrectly();
		// correct: PBKDF2$5$EKCGAQDceZxsFmlzwSGUpgmtmz+hT4n/hymcnD/OxG4GwMRyVluflVWsV7FtVFDk0dXQjFI=$SomeName
		File.WriteAllText(_authFileName, "PBKDF2$X$Whatever$SomeName");

		Assert.Throws<ArgumentException>(() => new AuthFile(_authFileName));
	}

	[Test]
	public void ValidatesCorrectly() {
		AuthFile authFile = new(_authFileName);
		authFile.AddUser(_username, _pw, Pbkdf2Hasher.Instance, 5);
		authFile.UserExists(_username).Should().BeTrue();
		authFile.UserExists("someName").Should().BeFalse();

		authFile.CheckAuth(_username, _pw, out Int32 authLevel).Should().Be(AuthResult.Authenticated);
		authLevel.Should().Be(5);
		authFile.CheckAuth(_username, _pwOther, out authLevel).Should().Be(AuthResult.Failed);
		authLevel.Should().Be(0);
		authFile.CheckAuth("WrongUserName", _pwOther, out authLevel).Should().Be(AuthResult.Failed);
		authLevel.Should().Be(0);

		authFile.ChangeUser(_username, 6);
		authFile.ChangePassword(_username, _pwOther, Pbkdf2Hasher.Instance);

		authFile.CheckAuth(_username, _pw, out authLevel).Should().Be(AuthResult.Failed);
		authLevel.Should().Be(0);
		authFile.CheckAuth(_username, _pwOther, out authLevel).Should().Be(AuthResult.Authenticated);
		authLevel.Should().Be(6);
	}

	[Test]
	public void HashingFunctions() {
		AuthFile authFile = new(_authFileName);
		authFile.AddUser(_username, _pw, Pbkdf2Hasher.Instance, 5);
		authFile.UserExists(_username).Should().BeTrue();
		authFile.CheckAuth(_username, _pw).Should().Be(AuthResult.Authenticated);
		authFile.ClearAllHashingFunctions();
		authFile.CheckAuth(_username, _pw).Should().Be(AuthResult.Failed);
		authFile.AddHashingFunction(Pbkdf2Hasher.Instance);
		authFile.CheckAuth(_username, _pw).Should().Be(AuthResult.Authenticated);
	}

	[Test]
	[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
	public void ThrowsOnInvalidData() {
		Assert.Throws<ArgumentNullException>(() => new AuthFile(null!));
		Assert.Throws<ArgumentNullException>(() => new AuthFile(String.Empty));
		AuthFile authFile = new(_authFileName);

		Assert.Throws<ArgumentException>(() => authFile.AddUser(String.Empty, _pw, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentNullException>(() => authFile.AddUser(null!, _pw, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentNullException>(() => authFile.AddUser(_username, null!, Pbkdf2Hasher.Instance));

		Assert.Throws<ArgumentException>(() => authFile.AddUser(_illegalUsername, _pw, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentException>(() => authFile.AddUser(_username, _pw, new IllegalHasher()));
		Assert.Throws<ArgumentNullException>(() => authFile.AddUser(_username, _pw, null!));

		authFile.AddUser(_username, _pw, Pbkdf2Hasher.Instance, 5);
		Assert.Throws<ArgumentException>(() => authFile.AddUser(_username, _pw, Pbkdf2Hasher.Instance, 5));

		authFile.ClearAllHashingFunctions();
		authFile.AddHashingFunction(Pbkdf2Hasher.Instance);
		Assert.Throws<ArgumentException>(() => authFile.AddHashingFunction(Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentException>(() => authFile.AddHashingFunction(new IllegalHasher()));
		Assert.Throws<ArgumentNullException>(() => authFile.AddHashingFunction(null!));
		Assert.Throws<ArgumentException>(() => authFile.AddHashingFunction(new IllegalHasher(null!)));

		Assert.Throws<ArgumentNullException>(() => authFile.ChangeUser(null!, -1));
		Assert.Throws<ArgumentException>(() => authFile.ChangeUser(String.Empty, -1));
		// Should not throw
		authFile.ChangeUser(_illegalUsername, 5);

		Assert.Throws<ArgumentNullException>(() => authFile.ChangePassword(null!, _pw, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentException>(() => authFile.ChangePassword(String.Empty, _pw, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentException>(() => authFile.ChangePassword(_illegalUsername, _pw, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentNullException>(() => authFile.ChangePassword(_username, null!, Pbkdf2Hasher.Instance));
		Assert.Throws<ArgumentNullException>(() => authFile.ChangePassword(_username, _pw, null!));
		Assert.Throws<ArgumentException>(() => authFile.ChangePassword(_username, _pw, new IllegalHasher()));
		Assert.Throws<ArgumentException>(() => authFile.ChangePassword("UnknownUser", _pw, Pbkdf2Hasher.Instance));
	}

	private class IllegalHasher : IPasswordHashingFunction {
		public IllegalHasher(String id = "Illegal#Id") {
			Id = id;
		}

		#region Implementation of IPasswordHashingFunction

		/// <inheritdoc />
		public String Id { get; }

		/// <inheritdoc />
		public String HashPassword(String username, String password) => throw new NotImplementedException();

		/// <inheritdoc />
		public Boolean VerifyPassword(String username, String password, String passwordHash) => throw new NotImplementedException();

		#endregion
	}
}