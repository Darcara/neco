namespace Neco.Common.Helper;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public static class ReflectionHelper {
	public static IEnumerable<PropertyInfo> GetPropertyWithAnyAttribute<T>(Type t) where T : Attribute {
		if (t == null) return Enumerable.Empty<PropertyInfo>();
		return t.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(prop => prop.GetCustomAttributes<T>(true).Any());
	}

	public static IEnumerable<MethodInfo> GetMethodWithAnyAttribute<T>(Type t) where T : Attribute {
		if (t == null) return Enumerable.Empty<MethodInfo>();

		return t.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(prop => prop.GetCustomAttributes<T>(true).Any());
	}

	public static T? GetPropertyValue<T>(Object obj, String propertyName) {
		PropertyInfo? property = obj
			.GetType()
			.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(prop => prop.Name == propertyName);

		if (property == null) return default;

		return (T?)property.GetValue(obj);
	}

	public static TReturn? GetStaticFieldOrPropertyValue<TReturn, TType>(String name) {
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

		return default(TReturn);
	}

	public static T? GetFieldOrPropertyValue<T>(Object obj, String name) {
		if (TryGetFieldOrPropertyValue(obj, name, out T? value))
			return value;

		throw new ArgumentException($"No field or property named {name} found", nameof(name));
	}

	public static Boolean TryGetFieldOrPropertyValue<T>(Object obj, String name, out T? value) {
		PropertyInfo? property = obj
			.GetType()
			.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(prop => prop.Name == name);

		if (property != null) {
			value = (T?)property.GetValue(obj);
			return true;
		}

		FieldInfo? field = obj
			.GetType()
			.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(field => field.Name == name);

		if (field != null) {
			value = (T?)field.GetValue(obj);
			return true;
		}

		value = default(T);
		return false;
	}

	public static T? SetFieldOrPropertyValue<T>(Object obj, String name, Boolean mustBeDefault, Func<T> factory) {
		if (TrySetFieldOrPropertyValue(obj, name, mustBeDefault, factory, out T? value)) {
			return value;
		}

		throw new ArgumentException($"No field or property named {name} found", nameof(name));
	}

	public static Boolean TrySetFieldOrPropertyValue<T>(Object obj, String name, Boolean mustBeDefault, Func<T> factory, out T? value) {
		ArgumentNullException.ThrowIfNull(obj, nameof(obj));
		ArgumentNullException.ThrowIfNull(factory, nameof(factory));

		PropertyInfo? property = obj
			.GetType()
			.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.FirstOrDefault(prop => prop.Name == name);

		if (property != null) {
			T? currentValue = (T?)property.GetValue(obj);
			if (currentValue == null || currentValue.Equals(default(T)) || !mustBeDefault) {
				T newValue = factory();
				property.SetValue(obj, newValue);
				currentValue = newValue;
			}

			value = currentValue;
			return true;
		}

		FieldInfo? field = obj
			.GetType()
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

		value = default(T);
		return false;
	}
}