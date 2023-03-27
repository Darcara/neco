namespace Neco.Test.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		Assert.That(methodInfos, Has.Count.EqualTo(6));
	}

	private abstract class ABaseClass<T> : IEquatable<T> {
		#region Implementation of IEquatable<T>

		/// <inheritdoc />
		public abstract Boolean Equals(T? other);

		#endregion
	}

	private sealed class ImplementingClass : ABaseClass<String> {
		#region Overrides of ABaseClass<string>

		/// <inheritdoc />
		public override Boolean Equals(String? other) => throw new InvalidOperationException();

		#endregion
	}
}