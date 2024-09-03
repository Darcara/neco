namespace Neco.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// The type extensions.
/// </summary>
public static partial class TypeExtensions {
	[GeneratedRegex("(`\\d+)|(\\[.*\\]$)")]
	private static partial Regex GenericTypeNameRegex();

	/// <summary>
	/// <para>Returns the correct name (without namespace) of the type.</para>
	/// <para>For generic types the name will be the name of the type without a generic hint</para>
	/// </summary>
	/// <param name="t">The type</param>
	/// <returns>The correct name of the type, without the generic hint</returns>
	public static String GetName(this Type? t) {
		if (t == null)
			return "null";
		if (!t.IsGenericType)
			return t.Name;
		return CleanName(t.Name);
	}

	/// <summary>
	/// <para>Returns the correct full name including the namespace of the type.</para>
	/// <para>For generic types the name will be the name of the type without a generic hint</para>
	/// </summary>
	/// <param name="t">The type</param>
	/// <returns>The correct name of the type, without the generic hint</returns>
	public static String GetFullName(this Type? t) {
		if (t == null)
			return "null";
		if (!t.IsGenericType)
			return t.FullName ?? t.GetName();

		if (t.IsNested)
			return GetCleanNestedFullName(t, false);

		if (!String.IsNullOrEmpty(t.Namespace) && !String.IsNullOrEmpty(t.FullName))
			return CleanName(t.FullName);

		return t.Namespace + "." + t.GetName();
	}

	/// <summary>
	/// <para>Returns the correct name (without namespace) of the type.</para>
	/// <para>For generic types the name will be the name of the type with the current generic type </para>
	/// </summary>
	/// <param name="t">The type</param>
	/// <returns>The correct name of the type, without the generic hint, like 'Generic&lt;SomeType&gt;'</returns>
	public static String GetGenericName(this Type? t) {
		if (t == null)
			return "null";
		if (!t.IsGenericType)
			return t.Name;
		return CleanNameGeneric(t.Name, "<", ">", t);
	}

	/// <summary>
	/// <para>Returns the correct name (without namespace) of the type.</para>
	/// <para>For generic types the name will be the name of the type with the current generic type </para>
	/// </summary>
	/// <param name="t">The type</param>
	/// <returns>The correct name of the type, without the generic hint</returns>
	public static String GetFullGenericName(this Type? t) {
		if (t == null)
			return "null";

		if (!t.IsGenericType)
			return t.GetFullName();

		if (t.IsNested)
			return GetCleanNestedFullName(t, true);

		if (!String.IsNullOrEmpty(t.Namespace) && !String.IsNullOrEmpty(t.FullName))
			return t.Namespace + "." + CleanNameGeneric(t.FullName.Substring(t.Namespace.Length + 1), "<", ">", t);

		return t.Namespace + "." + t.GetGenericName();
	}

	private static String CleanName(String s) {
		// Fullname for lambdas is Some.Namespace.ClassName`1+NestedClass`1+<>c__DisplayClass9_0
		// Name is '<>c__DisplayClass9_0' and it might be generic
		var idxGenericMark = s.IndexOf('`', StringComparison.Ordinal);
		if (idxGenericMark == -1)
			return s;
		return s.Substring(0, idxGenericMark);
	}

	private static String CleanNameGeneric(String s, String lp, String rp, Type t) {
		// Fullname for lambdas is Some.Namespace.ClassName`1+NestedClass`1+<>c__DisplayClass9_0
		// Name is '<>c__DisplayClass9_0' and it might be generic
		String cleanName = CleanName(s);
		Type[] genericTypeArguments = t.GenericTypeArguments;
		if (genericTypeArguments.Length == 0)
			return cleanName;
		return $"{cleanName}{lp}{String.Join(",", genericTypeArguments.WhereNotNull().Select(ta => ta.GetGenericName()))}{rp}";
	}

	private static String GetCleanNestedFullName(Type t, Boolean includeTypeParams) {
		String s = t.FullName ?? t.Name;
		String cleanName = GenericTypeNameRegex().Replace(s, "");
		if (!includeTypeParams) return cleanName;

		Type[] genericTypeArguments = t.GenericTypeArguments;
		if (genericTypeArguments.Length == 0)
			return cleanName;
		return $"{cleanName}<{String.Join(",", genericTypeArguments.WhereNotNull().Select(ta => ta.GetGenericName()))}>";
	}

	/// <summary>
	/// Checks if the given type, any of its base types, or any of its interfaces implements the given interface type.
	/// </summary>
	public static Boolean ImplementsInterface(this Type? t, Type ifaceType) {
		Type[] testTypeArguments = ifaceType.GenericTypeArguments;

		for (Type? type = t; type != null; type = type.BaseType) {
			Type[] interfaces = type.GetInterfaces();
			for (Int32 index = 0; index < interfaces.Length; ++index) {
				Type possibleMatch = interfaces[index];
				if (possibleMatch == ifaceType) return true;

				// One is generic, but not the other --> no match
				if (possibleMatch.IsGenericType != ifaceType.IsGenericType) continue;

				if (ifaceType.IsGenericType) {
					if (testTypeArguments.Length == 0) {
						// Check for any implementation of the generic interface --> typeof(Dictionary<,>)
						if (ifaceType.GetFullName() == possibleMatch.GetFullName()) return true;
					} else {
						// Check for any implementation of a specific generic interface --> typeof(Dictionary<Int32, String>)
						if (ifaceType.GetFullGenericName() == possibleMatch.GetFullGenericName()) return true;
					}
				}

				if (possibleMatch.ImplementsInterface(ifaceType))
					return true;
			}
		}

		return false;
	}

	public static Boolean ImplementsInterface(this Type t, String ifaceTypeFullName) {
		if (String.Equals(t.GetFullName(), ifaceTypeFullName, StringComparison.Ordinal) || String.Equals(t.GetFullGenericName(), ifaceTypeFullName, StringComparison.Ordinal))
			return true;

		for (Type? type = t; type != null; type = type.BaseType) {
			Type[] interfaces = type.GetInterfaces();
			for (Int32 index = 0; index < interfaces.Length; ++index) {
				Type possibleMatch = interfaces[index];
				if (String.Equals(possibleMatch.GetFullName(), ifaceTypeFullName, StringComparison.Ordinal) || String.Equals(possibleMatch.GetFullGenericName(), ifaceTypeFullName, StringComparison.Ordinal))
					return true;

				if (possibleMatch.ImplementsInterface(ifaceTypeFullName))
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="givenType"></param>
	/// <param name="genericType"></param>
	/// <returns></returns>
	public static Boolean IsAssignableToGenericType(this Type givenType, Type genericType) {
		Type[] interfaceTypes = givenType.GetInterfaces();

		foreach (Type it in interfaceTypes) {
			if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
				return true;
		}

		if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
			return true;

		Type? baseType = givenType.BaseType;
		if (baseType == null) return false;

		return IsAssignableToGenericType(baseType, genericType);
	}

	public static IEnumerable<MethodInfo> GetMethodsIncludingSuperInterfaces(this Type type) {
		foreach (MethodInfo? method in type.GetMethods()) {
			yield return method;
		}

		foreach (MethodInfo? method in type.GetInterfaces().SelectMany(GetMethodsIncludingSuperInterfaces)) {
			yield return method;
		}
	}
}