namespace Neco.Test.Common.Helper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public class ReflectionHelperTests {
	public static String? PublicStaticField;
	public static String? PublicStaticProperty { get; set; }
	public String? PublicField;
	public String? PublicProperty { get; set; }
	private String? _privateField;
	private String? PrivateProperty { get; set; }

	[TestCase(nameof(PublicField))]
	[TestCase(nameof(PublicProperty))]
	[TestCase(nameof(_privateField))]
	[TestCase(nameof(PrivateProperty))]
	public void GetSetInstanceFieldsOrProperties(String memberName) {
		ReflectionHelper.GetFieldOrPropertyValue<String>(this, memberName).Should().BeNull();
		ReflectionHelper.SetFieldOrPropertyValue(this, memberName, true, () => "Value");
		ReflectionHelper.GetFieldOrPropertyValue<String>(this, memberName).Should().Be("Value");
		ReflectionHelper.SetFieldOrPropertyValue(this, memberName, true, () => "AnotherValue");
		ReflectionHelper.GetFieldOrPropertyValue<String>(this, memberName).Should().Be("Value");
		ReflectionHelper.TrySetFieldOrPropertyValue(this, memberName, false, () => "AnotherValue", out String? newValue).Should().BeTrue();
		newValue.Should().Be("AnotherValue");
		ReflectionHelper.TryGetFieldOrPropertyValue<String>(this, memberName, out String? val).Should().BeTrue();
		val.Should().Be("AnotherValue");
	}

	[TestCase(nameof(PublicStaticField))]
	[TestCase(nameof(PublicStaticProperty))]
	public void GetSetStaticFieldsOrProperties(String memberName) {
		ReflectionHelper.GetStaticFieldOrPropertyValue<String, ReflectionHelperTests>(memberName).Should().BeNull();
	}

	[Test]
	public void GetSetAbstractFieldsOrProperties() {
		BaseClass obj = new();
		ReflectionHelper.GetFieldOrPropertyValue<String>(obj, nameof(IBaseInterface.PublicProperty)).Should().BeNull();
		ReflectionHelper.GetFieldOrPropertyValue<String>(obj, nameof(IBaseInterface.PublicSharedProperty)).Should().BeNull();
		ReflectionHelper.GetFieldOrPropertyValue<String>(obj, nameof(IBaseInterface.PublicAbstractProperty)).Should().BeNull();
		ReflectionHelper.GetFieldOrPropertyValue<String>(obj, nameof(IBaseInterface.PublicImplementedProperty)).Should().BeNull();
		ReflectionHelper.GetFieldOrPropertyValue<String>(obj, nameof(IBaseInterface.PublicOverriddenProperty)).Should().BeNull();
		ReflectionHelper.GetFieldOrPropertyValue<String>(obj, nameof(ABaseClass.PrivateSetterAttribute)).Should().BeNull();
		
		ReflectionHelper.SetFieldOrPropertyValue(obj, nameof(ABaseClass.PrivateSetterAttribute), true, () => "AnotherValue");

	}

	[Test]
	public void GetInvalidFieldsOrProperties() {
		Assert.Throws<ArgumentException>(() => ReflectionHelper.SetFieldOrPropertyValue(this, "invalid", false, () => "AnotherValue"));
		Assert.Throws<ArgumentException>(() => ReflectionHelper.GetFieldOrPropertyValue<String>(this, "invalid"));
		ReflectionHelper.TrySetFieldOrPropertyValue(this, "invalid", false, () => "AnotherValue", out _).Should().BeFalse();
		ReflectionHelper.TryGetFieldOrPropertyValue<String>(this, "invalid", out _).Should().BeFalse();

		Assert.Throws<ArgumentException>(() => ReflectionHelper.GetStaticFieldOrPropertyValue<String, String>("invalid").Should().BeNull());
	}

	[Test]
	public void GetMethodsWithAttribute() {
		List<MethodInfo> methods = ReflectionHelper.GetMethodWithAnyAttribute<CustomAttribute>(typeof(BaseClass)).ToList();
		methods.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicInterfaceMethod));
		methods.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicSharedMethod));
		methods.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicInterfaceToBeAbstractExtendedMethod));
		methods.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicInterfaceToBeImplementedMethod));
		methods.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicInterfaceToBeOverriddedMethod));
		methods.Should().Contain(m => m.Name == "ProtectedInterfaceMethod");
		methods.Should().Contain(m => m.Name == nameof(IAnotherInterface.PublicSharedMethod));
		methods.Should().Contain(m => m.Name == nameof(IAnotherInterface.AnotherPublicInterfaceMethod));
		methods.Should().Contain(m => m.Name == "AnotherProtectedInterfaceMethod");
		methods.Should().Contain(m => m.Name == nameof(ABaseClass.PublicSharedMethod));
		methods.Should().Contain(m => m.Name == nameof(ABaseClass.PublicInterfaceMethod));
		methods.Should().Contain(m => m.Name == nameof(ABaseClass.PublicInterfaceToBeAbstractExtendedMethod));
		methods.Should().Contain(m => m.Name == nameof(ABaseClass.PublicInterfaceToBeImplementedMethod));
		methods.Should().Contain(m => m.Name == nameof(ABaseClass.PublicInterfaceToBeOverriddedMethod));
		methods.Should().Contain(m => m.Name == nameof(BaseClass.PublicInterfaceToBeOverriddedMethod));

		methods.Should().NotContain(m => m.Name == nameof(IAnotherInterface.AnotherProtectedInterfaceMethodWithBody));
		methods.Should().NotContain(m => m.Name == nameof(IBaseInterface.ProtectedInterfaceMethodWithBody));
	}

	[Test]
	public void GetPropertiesWithAttribute() {
		List<PropertyInfo> properties = ReflectionHelper.GetPropertyWithAnyAttribute<CustomAttribute>(typeof(BaseClass)).ToList();
		// These are found twice
		properties.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicProperty));
		properties.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicImplementedProperty));
		properties.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicSharedProperty));
		// These are found once
		properties.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicOverriddenProperty));
		properties.Should().Contain(m => m.Name == nameof(IBaseInterface.PublicAbstractProperty));
		properties.Should().HaveCount(8);

		properties.Should().NotContain(m => m.Name == nameof(IBaseInterface.InterfaceProperty));
	}
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
internal sealed class CustomAttribute : Attribute {
}

internal interface IBaseInterface {
	[Custom] public String PublicProperty { get; set; }
	[Custom] public String PublicSharedProperty { get; set; }
	public String PublicAbstractProperty { get; set; }
	public String PublicImplementedProperty { get; set; }
	public String PublicOverriddenProperty { get; set; }

	[Custom] public String InterfaceProperty => PublicProperty;

	[Custom]
	public void PublicSharedMethod();

	[Custom]
	public void PublicInterfaceMethod();

	public void PublicInterfaceToBeAbstractExtendedMethod();
	public void PublicInterfaceToBeImplementedMethod();
	public void PublicInterfaceToBeOverriddedMethod();

	[Custom]
	protected void ProtectedInterfaceMethod();

	[Custom]
	public void ProtectedInterfaceMethodWithBody() {
		throw new NotImplementedException();
	}
}

internal interface IAnotherInterface {
	[Custom] public String PublicSharedProperty { get; set; }

	[Custom]
	public void PublicSharedMethod();

	[Custom]
	public void AnotherPublicInterfaceMethod();

	[Custom]
	protected void AnotherProtectedInterfaceMethod();

	[Custom]
	public void AnotherProtectedInterfaceMethodWithBody() {
	}
}

internal abstract class ABaseClass : IBaseInterface {
	#region Implementation of IBaseInterface

	/// <inheritdoc />
	public String PublicProperty { get; set; }

	/// <inheritdoc />
	public String PublicSharedProperty { get; set; }

	/// <inheritdoc />
	[Custom]
	public abstract String PublicAbstractProperty { get; set; }

	/// <inheritdoc />
	[Custom]
	public String PublicImplementedProperty { get; set; }

	/// <inheritdoc />
	public virtual String PublicOverriddenProperty { get; set; }
	public String PrivateSetterAttribute { get; private set; }

	/// <inheritdoc />
	public abstract void PublicSharedMethod();

	/// <inheritdoc />
	public abstract void PublicInterfaceMethod();

	/// <inheritdoc />
	[Custom]
	public abstract void PublicInterfaceToBeAbstractExtendedMethod();

	/// <inheritdoc />
	[Custom]
	public void PublicInterfaceToBeImplementedMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public virtual void PublicInterfaceToBeOverriddedMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public abstract void ProtectedInterfaceMethod();

	#endregion
}

internal class BaseClass : ABaseClass, IAnotherInterface {
	#region Overrides of ABaseClass

	/// <inheritdoc />
	public override String PublicAbstractProperty { get; set; }

	/// <inheritdoc />
	[Custom]
	public override String PublicOverriddenProperty { get; set; }

	public override void PublicSharedMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void AnotherPublicInterfaceMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public void AnotherProtectedInterfaceMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public override void PublicInterfaceMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public override void PublicInterfaceToBeAbstractExtendedMethod() {
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public override void ProtectedInterfaceMethod() {
		throw new NotImplementedException();
	}

	#endregion

	#region Overrides of ABaseClass

	/// <inheritdoc />
	[Custom]
	public override void PublicInterfaceToBeOverriddedMethod() {
		throw new NotImplementedException();
	}

	#endregion
}