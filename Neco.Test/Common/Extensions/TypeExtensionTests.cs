namespace Neco.Test.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class TypeExtensionTests {
	[Test]
	public void TypeNames() {
		Assert.That(typeof(Dictionary<String, String>).GetName(), Is.EqualTo("Dictionary"));
		Assert.That(typeof(Dictionary<String, String>).GetFullName(), Is.EqualTo("System.Collections.Generic.Dictionary"));
		Assert.That(typeof(Dictionary<String, String>).GetGenericName(), Is.EqualTo("Dictionary<String,String>"));
		Assert.That(typeof(Dictionary<String, String>).GetFullGenericName(), Is.EqualTo("System.Collections.Generic.Dictionary<String,String>"));

		Assert.That(typeof(TypeExtensionTests).GetName(), Is.EqualTo("TypeExtensionTests"));
		Assert.That(typeof(TypeExtensionTests).GetFullName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests"));
		Assert.That(typeof(TypeExtensionTests).GetGenericName(), Is.EqualTo("TypeExtensionTests"));
		Assert.That(typeof(TypeExtensionTests).GetFullGenericName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests"));

		Assert.That(typeof(ImplementingClass).GetName(), Is.EqualTo("ImplementingClass"));
		Assert.That(typeof(ImplementingClass).GetFullName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ImplementingClass"));
		Assert.That(typeof(ImplementingClass).GetGenericName(), Is.EqualTo("ImplementingClass"));
		Assert.That(typeof(ImplementingClass).GetFullGenericName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ImplementingClass"));

		Assert.That(typeof(ABaseClass<String>).GetName(), Is.EqualTo("ABaseClass"));
		Assert.That(typeof(ABaseClass<String>).GetFullName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass"));
		Assert.That(typeof(ABaseClass<String>).GetGenericName(), Is.EqualTo("ABaseClass<String>"));
		Assert.That(typeof(ABaseClass<String>).GetFullGenericName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass<String>"));

		// Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass`1+NestedClass`1+<>c__DisplayClass0_0
		Type nestedLambdaTypeRaw = typeof(TypeExtensionTests).Assembly.GetTypes().Single(t => t.FullName?.StartsWith("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass`1+NestedClass`1+<>c__DisplayClass", StringComparison.Ordinal) ?? false);
		Type nestedLambdaType = nestedLambdaTypeRaw.MakeGenericType(typeof(String), typeof(Int32));
		Assert.Multiple(() => {
			Assert.That(nestedLambdaType.GetName(), Does.StartWith("<>c__DisplayClass"));
			Assert.That(nestedLambdaType.GetFullName(), Does.StartWith("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass+NestedClass+<>c__DisplayClass"));
			Assert.That(nestedLambdaType.GetGenericName(), Does.StartWith("<>c__DisplayClass"));
			Assert.That(nestedLambdaType.GetGenericName(), Does.EndWith("<String,Int32>"));
			Assert.That(nestedLambdaType.GetFullGenericName(), Does.StartWith("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass+NestedClass+<>c__DisplayClass"));
			Assert.That(nestedLambdaType.GetFullGenericName(), Does.EndWith("<String,Int32>"));
		});

		Assert.That(((Type?)null).GetName(), Is.EqualTo("null"));
		Assert.That(((Type?)null).GetFullName(), Is.EqualTo("null"));
		Assert.That(((Type?)null).GetGenericName(), Is.EqualTo("null"));
		Assert.That(((Type?)null).GetFullGenericName(), Is.EqualTo("null"));
	}

	[Test]
	public void ImplementsInterface() {
		Assert.That(typeof(TypeExtensionTests).ImplementsInterface(typeof(Object)), Is.False);
		Assert.That(typeof(TypeExtensionTests).ImplementsInterface(typeof(IDictionary<String, String>)), Is.False);
		Assert.That(typeof(Dictionary<String, String>).ImplementsInterface(typeof(IDictionary<String, String>)), Is.True);
		Assert.That(typeof(Dictionary<String, String>).ImplementsInterface(typeof(IDictionary<String, Object>)), Is.False);
		Assert.That(typeof(Dictionary<String, String>).ImplementsInterface(typeof(IDictionary<Object, String>)), Is.False);
		Assert.That(typeof(Dictionary<String, String>).ImplementsInterface(typeof(IDictionary<Object, Object>)), Is.False);
		Assert.That(typeof(Dictionary<,>).ImplementsInterface(typeof(IDictionary<,>)), Is.True);
		Assert.That(typeof(Dictionary<String, String>).ImplementsInterface(typeof(IDictionary<,>)), Is.True);
		Assert.That(typeof(Dictionary<,>).ImplementsInterface(typeof(IDictionary<String, String>)), Is.False);
		Assert.That(typeof(Dictionary<,>).ImplementsInterface(typeof(ICollection<>)), Is.True);
	}

	[Test]
	public void IsAssignableToGenericType() {
		Assert.That(typeof(Dictionary<String, String>).IsAssignableTo(typeof(Dictionary<String, String>)), Is.True);
		Assert.That(typeof(Dictionary<String, String>).IsAssignableTo(typeof(Dictionary<,>)), Is.False);
		Assert.That(typeof(Dictionary<String, String>).IsAssignableToGenericType(typeof(Dictionary<,>)), Is.True);
		Assert.That(typeof(Dictionary<,>).IsAssignableToGenericType(typeof(Dictionary<,>)), Is.True);
		Assert.That(typeof(ImplementingClass).IsAssignableToGenericType(typeof(IEquatable<>)), Is.True);
		Assert.That(typeof(ImplementingClass).IsAssignableToGenericType(typeof(ABaseClass<>)), Is.True);
		Assert.That(typeof(ImplementingClass).IsAssignableToGenericType(typeof(Dictionary<,>)), Is.False);
	}

	[Test]
	public void MethodsIncludingSuperInterfaces() {
		List<MethodInfo> methodInfos = typeof(ImplementingClass).GetMethodsIncludingSuperInterfaces().ToList();
		methodInfos.ForEach(mi => Console.WriteLine($"{mi} from {mi.DeclaringType.GetName()} at {mi.ReflectedType.GetName()}"));
		Assert.That(methodInfos, Has.Count.EqualTo(8));
	}

	[Test]
	public void GetCustomAttributes() {
		typeof(ImplementingClass)
			.GetCustomAttributesIncludingBaseInterfaces<SingleAttribute>()
			.Should().HaveCount(3);
		typeof(ImplementingClass)
			.GetCustomAttributesIncludingBaseInterfaces<MultiAttribute>()
			.Should().HaveCount(3);
		typeof(ImplementingClass)
			.GetCustomAttributesIncludingBaseInterfaces<SingleNonInheritingAttribute>()
			.Should().HaveCount(3);
	}

	[Test]
	public void GetCustomAttributesForMember() {
		MethodInfo? methodInfo = typeof(ImplementingClass).GetMethod(nameof(IInterface.SomeMethod));
		Assert.That(methodInfo, Is.Not.Null);

		methodInfo
			.GetCustomAttributesIncludingBaseInterfaces<SingleAttribute>()
			.Should().HaveCount(3);

		methodInfo
			.GetCustomAttributesIncludingBaseInterfaces(nameof(MultiAttribute))
			.Should().HaveCount(3);

		methodInfo
			.GetCustomAttributesIncludingBaseInterfaces(typeof(SingleNonInheritingAttribute))
			.Should().HaveCount(3);
	}

	[Single("Interface")]
	[SingleNonInheriting("Interface")]
	[Multi("Interface")]
	private interface IInterface {
		[Single("Interface.SomeMethod")]
		[SingleNonInheriting("Interface.SomeMethod")]
		[Multi("Interface.SomeMethod")]
		public void SomeMethod();
	}

	[Single("ABaseClass")]
	[SingleNonInheriting("ABaseClass")]
	[Multi("ABaseClass")]
	private abstract class ABaseClass<T> : IInterface, IEquatable<T> {
		#region Implementation of IEquatable<T>

		/// <inheritdoc />
		public abstract Boolean Equals(T? other);

		#endregion

		private class NestedClass<TS> {
			protected T? MethodWithLambda(T someT, TS someS) {
				HashSet<T> someSet = new();
				return someSet.FirstOrDefault(s => s.Equals(someT) || s.Equals(someS));
			}
		}

		#region Implementation of IInterface

		/// <inheritdoc />
		[Single("ABaseClass.SomeMethod")]
		[SingleNonInheriting("ABaseClass.SomeMethod")]
		[Multi("ABaseClass.SomeMethod")]
		public abstract void SomeMethod();

		#endregion
	}

	[Single("ImplementingClass")]
	[SingleNonInheriting("ImplementingClass")]
	[Multi("ImplementingClass")]
	private sealed class ImplementingClass : ABaseClass<String> {
		#region Overrides of ABaseClass<string>

		/// <inheritdoc />
		public override Boolean Equals(String? other) => throw new InvalidOperationException();

		/// <inheritdoc />
		[Single("ImplementingClass.SomeMethod")]
		[SingleNonInheriting("ImplementingClass.SomeMethod")]
		[Multi("ImplementingClass.SomeMethod")]
		public override void SomeMethod() => throw new NotImplementedException();

		#endregion
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	private sealed class SingleAttribute(String Data) : Attribute {
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	private sealed class SingleNonInheritingAttribute(String Data) : Attribute {
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	private sealed class MultiAttribute(String Data) : Attribute {
	}
}