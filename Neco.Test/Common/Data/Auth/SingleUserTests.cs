namespace Neco.Test.Common.Data.Auth;

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Neco.Common.Data.Auth;
using NUnit.Framework;

[TestFixture]
public class SingleUserTests {
	[TestCase("WrongUser", "", AuthResult.Failed)]
	[TestCase("TestUser", "", AuthResult.Failed)]
	[TestCase("TestUser", "wrongPassword", AuthResult.Failed)]
	[TestCase("TestUser", "testPassword", AuthResult.Failed)]
	[TestCase("testUser", "TestPassword", AuthResult.Failed)]
	[TestCase("TestUser", "TestPassword", AuthResult.Authenticated)]
	public void AuthorizesCorrectly(String user, String pw, AuthResult result) {
		IAuthenticationProvider auth = new SingleUser("TestUser", "TestPassword");
		auth.CheckAuth(user, pw).Should().Be(result);
	}

	[Test]
	[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
	public void InterfaceTests() {
		Assert.Throws<ArgumentNullException>(() => new SingleUser(null!, String.Empty));
		Assert.Throws<ArgumentNullException>(() => new SingleUser(String.Empty, null!));
		Assert.Throws<ArgumentNullException>(() => new SingleUser(null!, null!));
		
		IAuthenticationProvider auth = new SingleUser("TestUser", "TestPassword");
		Assert.Throws<ArgumentNullException>(() => auth.CheckAuth(null!, String.Empty));
		Assert.Throws<ArgumentNullException>(() => auth.CheckAuth(String.Empty, null!));
		Assert.Throws<ArgumentNullException>(() => auth.CheckAuth(null!, null!));

	}
}