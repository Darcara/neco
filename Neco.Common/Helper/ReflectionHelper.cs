namespace Neco.Common.Helper;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Neco.Common.Extensions;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public static class ReflectionHelper {
	/// <summary>
	/// Returns all properties (including from base classes) that are annotated with the given attribute type.
	/// </summary>
	[RequiresUnreferencedCode("Inspecting members might require types that cannot be statically analyzed.")]
	public static IEnumerable<PropertyInfo> GetPropertyWithAnyAttribute<TAttribute>(Type typeToInspect) where TAttribute : Attribute {
		ArgumentNullException.ThrowIfNull(typeToInspect);
		HashSet<PropertyInfo> properties = typeToInspect.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).ToHashSet();
		Type currentType = typeToInspect;
		while (currentType.BaseType != null && currentType.BaseType != typeof(Object)) {
			foreach (PropertyInfo propertyInfo in currentType.BaseType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)) {
				properties.Add(propertyInfo);
			}

			currentType = currentType.BaseType;
		}

		return properties.Where(prop => prop.GetCustomAttributesIncludingBaseInterfaces<TAttribute>().Any());
	}

	/// <summary>
	/// Returns all methods (including from base classes) that are annotated with the given attribute type.
	/// </summary>
	[RequiresUnreferencedCode("Inspecting members might require types that cannot be statically analyzed.")]
	public static IEnumerable<MethodInfo> GetMethodWithAnyAttribute<TAttribute>(Type typeToInspect) where TAttribute : Attribute {
		ArgumentNullException.ThrowIfNull(typeToInspect);

		return typeToInspect
			.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.Where(prop => prop.GetCustomAttributesIncludingBaseInterfaces<TAttribute>().Any());
	}

	/// <summary>
	/// Returns the current value of a static field or property with the given name
	/// </summary>
	/// <exception cref="ArgumentException">If no static member is found with the given name</exception>
	public static TReturn? GetStaticFieldOrPropertyValue<TReturn, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TType>(String name) {
		PropertyInfo? property = typeof(TType)
			.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(prop => prop.Name == name);

		if (property != null)
			return (TReturn?)property.GetValue(null);

		FieldInfo? field = typeof(TType)
			.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(field => field.Name == name);

		if (field != null)
			return (TReturn?)field.GetValue(null);

		throw new ArgumentException($"No static field or property named {name} found", nameof(name));
	}

	/// <summary>
	/// Returns the current value of an instance field or property with the given name
	/// </summary>
	/// <exception cref="ArgumentException">If no member is found with the given name</exception>
	public static T? GetFieldOrPropertyValue<T>(Object obj, String name) {
		if (TryGetFieldOrPropertyValue(obj, name, out T? value))
			return value;

		throw new ArgumentException($"No field or property named {name} found on {obj.GetType().GetName()}", nameof(name));
	}

	/// <summary>
	/// Returns wether an instance field or property with the given name has been found and its current value
	/// </summary>
	public static Boolean TryGetFieldOrPropertyValue<T>(Object obj, String name, out T? value) {
		PropertyInfo? property = obj
			.GetType()
			.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(prop => prop.Name == name);

		if (property != null) {
			value = (T?)property.GetValue(obj);
			return true;
		}

		FieldInfo? field = obj
			.GetType()
			.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(field => field.Name == name);

		if (field != null) {
			value = (T?)field.GetValue(obj);
			return true;
		}

		value = default(T);
		return false;
	}

	/// <summary>
	/// Sets instance field or property with the given name to the given value
	/// </summary>
	/// <exception cref="ArgumentException">If no member is found with the given name</exception>
	public static TValue? SetFieldOrPropertyValue<TValue>(Object obj, String name, Boolean mustBeDefault, Func<TValue> factory) {
		if (TrySetFieldOrPropertyValue(obj, name, mustBeDefault, factory, out TValue? value)) {
			return value;
		}

		throw new ArgumentException($"No field or property named {name} found on {obj.GetType().GetName()}", nameof(name));
	}

	/// <summary>
	/// Returns wether an instance field or property with the given name has been found and the value that has been set
	/// </summary>
	public static Boolean TrySetFieldOrPropertyValue<T>(Object obj, String name, Boolean mustBeDefault, Func<T> factory, out T? value) {
		return TrySetFieldOrPropertyValue(obj.GetType(), obj, name, mustBeDefault, factory, out value);
	}

	private static Boolean TrySetFieldOrPropertyValue<T>(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		Type objectType,
		Object obj,
		String name,
		Boolean mustBeDefault,
		Func<T> factory,
		out T? value) {
		ArgumentNullException.ThrowIfNull(obj, nameof(obj));
		ArgumentNullException.ThrowIfNull(factory, nameof(factory));

		PropertyInfo? property = objectType
			.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(prop => prop.Name == name);

		if (property != null && property.CanWrite) {
			T? currentValue = (T?)property.GetValue(obj);
			if (currentValue == null || currentValue.Equals(default(T)) || !mustBeDefault) {
				T newValue = factory();
				property.SetValue(obj, newValue);
				currentValue = newValue;
			}

			value = currentValue;
			return true;
		}

		FieldInfo? field = objectType
			.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(field => field.Name == name);

		if (field != null) {
			T? currentValue = (T?)field.GetValue(obj);
			if (currentValue == null || currentValue.Equals(default(T)) || !mustBeDefault) {
				T newValue = factory();
				field.SetValue(obj, newValue);
				currentValue = newValue;
			}

			value = currentValue;
			return true;
		}

		if (objectType.BaseType != null && objectType.BaseType != typeof(Object)) {
			return TrySetFieldOrPropertyValue(objectType.BaseType, obj, name, mustBeDefault, factory, out value);
		}

		value = default(T);
		return false;
	}
}