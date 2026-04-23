namespace Neco.Test.Common.Extensions;

using System.Reflection;
using Neco.Common.Extensions;

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
		using (Assert.EnterMultipleScope()) {
			Assert.That(typeof(ImplementingClass).GetCustomAttributesIncludingBaseInterfaces<SingleAttribute>(), Has.Exactly(4).Items);
			Assert.That(typeof(ImplementingClass).GetCustomAttributesIncludingBaseInterfaces<MultiAttribute>(), Has.Exactly(4).Items);
			Assert.That(typeof(ImplementingClass).GetCustomAttributesIncludingBaseInterfaces<SingleNonInheritingAttribute>(), Has.Exactly(4).Items);
		}
	}

	[TestCase(nameof(IInterface.SomeMethod), 3)]
	[TestCase("PrivateMethod", 1)]
	[TestCase("ProtectedMethod", 2)]
	[TestCase("PrivateProperty", 1)]
	[TestCase("ProtectedProperty", 2)]
	[TestCase("PrivateField", 1)]
	public void GetCustomAttributesForMember(String member, Int32 numberOfAttributes) {
		MemberInfo methodInfo = typeof(ImplementingClass).GetMember(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
		Assert.That(methodInfo, Is.Not.Null);

		using (Assert.EnterMultipleScope()) {
			Assert.That(methodInfo.GetCustomAttributesIncludingBaseInterfaces<SingleAttribute>(), Has.Exactly(numberOfAttributes).Items);
			Assert.That(methodInfo.GetCustomAttributesIncludingBaseInterfaces<MultiAttribute>(), Has.Exactly(numberOfAttributes).Items);
			Assert.That(methodInfo.GetCustomAttributesIncludingBaseInterfaces<SingleNonInheritingAttribute>(), Has.Exactly(numberOfAttributes).Items);
		}
	}

	[Single("IBaseInterface")]
	[SingleNonInheriting("IBaseInterface")]
	[Multi("IBaseInterface")]
	private interface IBaseInterface {
		[Single("IBaseInterface.SomeMethod")]
		[SingleNonInheriting("IBaseInterface.SomeMethod")]
		[Multi("IBaseInterface.SomeMethod")]
		public void SomeMethod();
	}

	[Single("Interface")]
	[SingleNonInheriting("Interface")]
	[Multi("Interface")]
	private interface IInterface : IBaseInterface {

	}

	[Single("ABaseClass")]
	[SingleNonInheriting("ABaseClass")]
	[Multi("ABaseClass")]
	private abstract class ABaseClass<T> : IInterface, IEquatable<T> {
		#region Implementation of IEquatable<T>

		/// <inheritdoc />
		public abstract Boolean Equals(T? other);

		#endregion

		[Single("ABaseClass.ProtectedProperty")]
		[SingleNonInheriting("ABaseClass.ProtectedProperty")]
		[Multi("ABaseClass.ProtectedProperty")]
		protected virtual String ProtectedProperty { get; set; }

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

		[Single("ABaseClass.ProtectedMethod")]
		[SingleNonInheriting("ABaseClass.ProtectedMethod")]
		[Multi("ABaseClass.ProtectedMethod")]
		protected virtual void ProtectedMethod() => throw new NotImplementedException();

		#endregion
	}

	[Single("ImplementingClass")]
	[SingleNonInheriting("ImplementingClass")]
	[Multi("ImplementingClass")]
	private sealed class ImplementingClass : ABaseClass<String> {
		[Single("ImplementingClass.PrivateField")] [SingleNonInheriting("ImplementingClass.PrivateField")] [Multi("ImplementingClass.PrivateField")]
		private String? PrivateField;

		[Single("ImplementingClass.PrivateProperty")]
		[SingleNonInheriting("ImplementingClass.PrivateProperty")]
		[Multi("ImplementingClass.PrivateProperty")]
		private String? PrivateProperty { get; set; }

		[Single("ImplementingClass.ProtectedProperty")]
		[SingleNonInheriting("ImplementingClass.ProtectedProperty")]
		[Multi("ImplementingClass.ProtectedProperty")]
		protected override String ProtectedProperty { get; set; }

		#region Overrides of ABaseClass<string>

		/// <inheritdoc />
		public override Boolean Equals(String? other) => throw new InvalidOperationException();

		/// <inheritdoc />
		[Single("ImplementingClass.SomeMethod")]
		[SingleNonInheriting("ImplementingClass.SomeMethod")]
		[Multi("ImplementingClass.SomeMethod")]
		public override void SomeMethod() => throw new NotImplementedException();

		[Single("ImplementingClass.ProtectedMethod")]
		[SingleNonInheriting("ImplementingClass.ProtectedMethod")]
		[Multi("ImplementingClass.ProtectedMethod")]
		protected override void ProtectedMethod() => throw new NotImplementedException();

		[Single("ImplementingClass.PrivateMethod")]
		[SingleNonInheriting("ImplementingClass.PrivateMethod")]
		[Multi("ImplementingClass.PrivateMethod")]
		private void PrivateMethod() => throw new NotImplementedException();

		#endregion
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	private sealed class SingleAttribute(String Data) : Attribute {
		public override String ToString() => Data;
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	private sealed class SingleNonInheritingAttribute(String Data) : Attribute {
		public override String ToString() => Data;
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	private sealed class MultiAttribute(String Data) : Attribute {
		public override String ToString() => Data;
	}
}