namespace Neco.Common.Extensions;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

public static class DictionaryExtensions {
	/// <inheritdoc cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey, TValue)"/>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
		ArgumentNullException.ThrowIfNull(dictionary);

		if (!dictionary.TryGetValue(key, out TValue? existingValue)) {
			dictionary.Add(key, value);
			return value;
		}

		return existingValue;
	}

	/// <inheritdoc cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey, Func{TKey,TValue})"/>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory) {
		ArgumentNullException.ThrowIfNull(dictionary);
		ArgumentNullException.ThrowIfNull(valueFactory);
		if (!dictionary.TryGetValue(key, out TValue? value)) {
			value = valueFactory(key);
			dictionary.Add(key, value);
		}

		return value;
	}

	/// <inheritdoc cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd{TArg}(TKey, Func{TKey, TArg,TValue}, TArg)"/>
	public static TValue GetOrAdd<TKey, TArg, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument) {
		ArgumentNullException.ThrowIfNull(dictionary);
		ArgumentNullException.ThrowIfNull(valueFactory);
		if (!dictionary.TryGetValue(key, out TValue? value)) {
			value = valueFactory(key, factoryArgument);
			dictionary.Add(key, value);
		}

		return value;
	}

	/// <summary>
	/// Appends a Value to a Dictionary that holds value lists
	/// </summary>
	public static void AppendValue<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) {
		ArgumentNullException.ThrowIfNull(dictionary);
		if (!dictionary.TryGetValue(key, out List<TValue>? valueList)) {
			valueList = new List<TValue>();
			dictionary.Add(key, valueList);
		}

		valueList.Add(value);
	}

	/// <inheritdoc cref="GetAs"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 GetAsInt32<TKey>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Number) => GetAs<TKey, Int32>(dictionary, key, styles);

	/// <inheritdoc cref="GetAs"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UInt32 GetAsUInt32<TKey>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Number) => GetAs<TKey, UInt32>(dictionary, key, styles);

	/// <inheritdoc cref="GetAs"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int64 GetAsInt64<TKey>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Number) => GetAs<TKey, Int64>(dictionary, key, styles);

	/// <inheritdoc cref="GetAs"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UInt64 GetAsUInt64<TKey>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Number) => GetAs<TKey, UInt64>(dictionary, key, styles);

	/// <inheritdoc cref="GetAs"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Single GetAsSingle<TKey>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands) => GetAs<TKey, Single>(dictionary, key, styles);

	/// <inheritdoc cref="GetAs"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Double GetAsDouble<TKey>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands) => GetAs<TKey, Double>(dictionary, key, styles);

	/// <summary>
	/// Returns the Dictionary-String value as the parsed primitive.
	/// Parsing will always use <see cref="NumberFormatInfo.InvariantInfo"/>
	/// </summary>
	public static TValue GetAs<TKey, TValue>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, NumberStyles styles = NumberStyles.Float | NumberStyles.Number) where TValue : INumber<TValue> {
		ArgumentNullException.ThrowIfNull(dictionary);
		return TValue.Parse(dictionary[key], styles, NumberFormatInfo.InvariantInfo);
	}

	/// <summary>
	/// Returns the Dictionary-String value as the parsed primitive.
	/// Parsing will always use <see cref="NumberFormatInfo.InvariantInfo"/>
	/// </summary>
	/// <returns>true if the Dictionary contains an element that has the specified key and it was parsed successfully; otherwise, false.</returns>
	public static Boolean TryGetAs<TKey, TValue>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value, NumberStyles styles = NumberStyles.Float | NumberStyles.Number) where TValue : INumber<TValue> {
		ArgumentNullException.ThrowIfNull(dictionary);
		if (!dictionary.TryGetValue(key, out String? valueStr)) {
			value = TValue.Zero;
			return false;
		}

		return TValue.TryParse(valueStr, styles, NumberFormatInfo.InvariantInfo, out value);
	}

	/// <summary>
	/// Returns the Dictionary-String value as the parsed primitive or the provided default value if the Dictionary is missing the key, or it's value could not be parsed.
	/// Parsing will always use <see cref="NumberFormatInfo.InvariantInfo"/>
	/// </summary>
	public static TValue GetAsOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, String> dictionary, TKey key, TValue defaultValue, NumberStyles styles = NumberStyles.Float | NumberStyles.Number) where TValue : INumber<TValue> {
		ArgumentNullException.ThrowIfNull(dictionary);
		return TryGetAs(dictionary, key, out TValue value, styles) ? value : defaultValue;
	}
}