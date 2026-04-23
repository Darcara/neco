namespace Neco.Common.Extensions;

using System.Diagnostics.CodeAnalysis;
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
	[RequiresUnreferencedCode("Inspecting members might require types that cannot be statically analyzed.")]
	public static IEnumerable<T> GetCustomAttributesIncludingBaseInterfaces<T>(this MemberInfo mi) => GetCustomAttributesIncludingBaseInterfaces(mi, typeof(T)).Cast<T>();

	/// <summary>
	/// <para>Returns the custom attributes including attributes from implemented interfaces for the given method.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	[RequiresUnreferencedCode("Inspecting members might require types that cannot be statically analyzed.")]
	[SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields")]
	public static IEnumerable<Attribute> GetCustomAttributesIncludingBaseInterfaces(this MemberInfo mi, Type attributeType) {
		ArgumentNullException.ThrowIfNull(mi);
		ArgumentNullException.ThrowIfNull(attributeType);

		foreach (Attribute customAttribute in mi.GetCustomAttributes(attributeType, false))
			yield return customAttribute;

		Type? reflectedType = mi.ReflectedType;
		if (reflectedType == null)
			yield break;

		Type[] implementedInterfaces = reflectedType.GetInterfaces();
		Func<Type, MemberInfo?> memberSelector;
		if (mi.MemberType.HasFlag(MemberTypes.Method) && mi is MethodInfo methodInfo) {
			Type[] methodParameters = methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
			memberSelector = iface => iface.GetMethod(methodInfo.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, methodParameters);
		} else if (mi.MemberType.HasFlag(MemberTypes.Property) && mi is PropertyInfo _) {
			memberSelector = iface => iface.GetProperty(mi.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		} else {
			memberSelector = iface => iface.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(m => String.Equals(m.Name, mi.Name, StringComparison.Ordinal))
				.SingleOrDefault(m => m.MemberType == mi.MemberType);
		}

		foreach (Attribute customAttribute in implementedInterfaces.Select(memberSelector).WhereNotNull().SelectMany(m => m.GetCustomAttributes(attributeType, true))) {
			yield return customAttribute;
		}

		Type? baseType = reflectedType;
		do {
			baseType = baseType.BaseType;
			if (baseType == null || baseType == typeof(Object))
				break;

			foreach (Attribute customAttribute in memberSelector(baseType)?.GetCustomAttributes(attributeType, false) ?? Array.Empty<Attribute>()) {
				yield return customAttribute;
			}
		} while (baseType != typeof(Object));
	}

	/// <summary>
	/// <para>Gets the custom attributes including attributes from implemented interfaces.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	public static IEnumerable<T> GetCustomAttributesIncludingBaseInterfaces<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type) where T : Attribute {
		ArgumentNullException.ThrowIfNull(type);

		foreach (T customAttribute in type.GetCustomAttributes<T>(false)) {
			yield return customAttribute;
		}

		foreach (Type implementedInterface in type.GetInterfaces()) {
			foreach (T customAttribute in implementedInterface.GetCustomAttributes<T>(false)) {
				yield return customAttribute;
			}
		}

		Type? baseType = type;
		do {
			baseType = baseType.BaseType;
			if (baseType == null || baseType == typeof(Object))
				break;

			foreach (T customAttribute in baseType.GetCustomAttributes<T>(false)) {
				yield return customAttribute;
			}
		} while (baseType != typeof(Object));
	}

	/// <summary>
	/// <para>Returns the custom attributes including attributes from implemented interfaces for the given method.</para>
	/// <para>If the same attribute is defined in multiple locations, it will be contained multiple times</para>
	/// </summary>
	[RequiresUnreferencedCode("Inspecting members might require types that cannot be statically analyzed.")]
	public static IEnumerable<Attribute> GetCustomAttributesIncludingBaseInterfaces(this MemberInfo mi, String fullAttributeTypeName) {
		return mi
			.GetCustomAttributesIncludingBaseInterfaces<Attribute>()
			.Where(attribute => String.Equals(fullAttributeTypeName, attribute.GetType().GetFullName(), StringComparison.Ordinal) || String.Equals(fullAttributeTypeName, attribute.GetType().GetFullGenericName(), StringComparison.Ordinal));
	}
}