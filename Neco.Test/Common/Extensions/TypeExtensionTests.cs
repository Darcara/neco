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

		Assert.That(typeof(ImplementingClass).GetName(), Is.EqualTo("ImplementingClass"));
		Assert.That(typeof(ImplementingClass).GetFullName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ImplementingClass"));
		Assert.That(typeof(ImplementingClass).GetGenericName(), Is.EqualTo("ImplementingClass"));
		Assert.That(typeof(ImplementingClass).GetFullGenericName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ImplementingClass"));

		Assert.That(typeof(ABaseClass<String>).GetName(), Is.EqualTo("ABaseClass"));
		Assert.That(typeof(ABaseClass<String>).GetFullName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass"));
		Assert.That(typeof(ABaseClass<String>).GetGenericName(), Is.EqualTo("ABaseClass<String>"));
		Assert.That(typeof(ABaseClass<String>).GetFullGenericName(), Is.EqualTo("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass<String>"));

		// Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass`1+NestedClass`1+<>c__DisplayClass0_0
		Type nestedLambdaTypeRaw = typeof(TypeExtensionTests).Assembly.GetTypes().Single(t => t.FullName.StartsWith("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass`1+NestedClass`1+<>c__DisplayClass", StringComparison.Ordinal));
		Type nestedLambdaType = nestedLambdaTypeRaw.MakeGenericType(typeof(String), typeof(Int32));
		Assert.Multiple(() => {
			StringAssert.StartsWith("<>c__DisplayClass", nestedLambdaType.GetName());
			StringAssert.StartsWith("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass+NestedClass+<>c__DisplayClass",nestedLambdaType.GetFullName());
			StringAssert.StartsWith("<>c__DisplayClass",nestedLambdaType.GetGenericName());
			StringAssert.EndsWith("<String,Int32>",nestedLambdaType.GetGenericName());
			StringAssert.StartsWith("Neco.Test.Common.Extensions.TypeExtensionTests+ABaseClass+NestedClass+<>c__DisplayClass", nestedLambdaType.GetFullGenericName());
			StringAssert.EndsWith("<String,Int32>",nestedLambdaType.GetFullGenericName());
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
		Assert.That(methodInfos, Has.Count.EqualTo(6));
	}

	private abstract class ABaseClass<T> : IEquatable<T> {
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
	}

	private sealed class ImplementingClass : ABaseClass<String> {
		#region Overrides of ABaseClass<string>

		/// <inheritdoc />
		public override Boolean Equals(String? other) => throw new InvalidOperationException();

		#endregion
	}
}