namespace Neco.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// <para>Extensions for getting attributes properly from reflected types/members/etc.</para>
/// <para>Why: Attributes on implemented Interface anythings are not available through <see cref="System.Reflection.MemberInfo.GetCustomAttributes(Type,bool)"/></para>
/// </summary>
public static partial class TypeExtensions {
	/// <summary>
	/// <para>Returns the custom attributes including attributes from implemented interfaces for the given method.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	public static IEnumerable<T> GetCustomAttributesIncludingBaseInterfaces<T>(this MemberInfo mi) => GetCustomAttributesIncludingBaseInterfaces(mi, typeof(T)).Cast<T>();

	/// <summary>
	/// <para>Returns the custom attributes including attributes from implemented interfaces for the given method.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	public static IEnumerable<Attribute> GetCustomAttributesIncludingBaseInterfaces(this MemberInfo mi, Type attributeType) {
		IEnumerable<Attribute> baseAndInheritedAttributes = mi.GetCustomAttributes(attributeType, true).Cast<Attribute>();
		Type? reflectedType = mi.ReflectedType;
		if (reflectedType == null)
			return baseAndInheritedAttributes;

		Type[] implementedInterfaces = reflectedType.GetInterfaces();
		IEnumerable<MemberInfo?> baseMembers;
		if (mi.MemberType.HasFlag(MemberTypes.Method) && mi is MethodInfo methodInfo) {
			Type[] methodParameters = methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
			baseMembers = implementedInterfaces.Select(iface => iface.GetMethod(methodInfo.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, methodParameters));
		} else if (mi.MemberType.HasFlag(MemberTypes.Property) && mi is PropertyInfo _) {
			baseMembers = implementedInterfaces.Select(iface => iface.GetProperty(mi.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
		} else {
			baseMembers = implementedInterfaces
				.SelectMany(iface => iface.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
				.Where(m => String.Equals(m.Name, mi.Name, StringComparison.Ordinal))
				.Where(m => m.MemberType == mi.MemberType);
		}

		IEnumerable<Attribute> baseAttributes = baseMembers.WhereNotNull().SelectMany(m => m.GetCustomAttributes(attributeType, true).Cast<Attribute>());
		return baseAndInheritedAttributes.Union(baseAttributes).Distinct();
	}

	/// <summary>
	/// <para>Gets the custom attributes including attributes from implemented interfaces.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	public static IEnumerable<T> GetCustomAttributesIncludingBaseInterfaces<T>(this Type type) {
		Type attributeType = typeof(T);
		return type.GetCustomAttributes(attributeType, true).Union(type.GetInterfaces().SelectMany(interfaceType => interfaceType.GetCustomAttributes(attributeType, true))).Cast<T>();
	}

	/// <summary>
	/// <para>Returns the custom attributes including attributes from implemented interfaces for the given method.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	public static IEnumerable<Attribute> GetCustomAttributesIncludingBaseInterfaces(this MemberInfo mi, String fullAttributeTypeName) {
		return mi
			.GetCustomAttributesIncludingBaseInterfaces(typeof(Attribute))
			.Where(attribute => String.Equals(fullAttributeTypeName, attribute.GetType().GetFullName()) || String.Equals(fullAttributeTypeName, attribute.GetType().GetFullGenericName()));
	}
}