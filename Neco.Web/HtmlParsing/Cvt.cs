namespace Neco.Web.HtmlParsing;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

public static class Cvt {
	private static readonly Char[] _basicNumberChars = "0123456789. -".ToCharArray();

	public static Object SingleNodeText(IEnumerable<HtmlNode> nodes) {
		return nodes.Single().InnerText.Trim();
	}

	public static Object SingleNodeTextHtmlDecoded(IEnumerable<HtmlNode> nodes) {
		return HttpUtility.HtmlDecode(nodes.Single().InnerText.Trim());
	}

	public static Func<HtmlNodeCollection, Object?> SingleNodeMapping<T>(IHtmlToPoco<T> mapping) where T : new() {
		return nodes => {
			T t = new();
			if (!mapping.Parse(nodes.Single(), t))
				return null;
			return t;
		};
	}

	public static Object ListNodeText(IEnumerable<HtmlNode> nodes) {
		return nodes.Select(n => n.InnerText).Where(t => !String.IsNullOrEmpty(t)).ToList();
	}

	public static Object? SingleNodeTextToInt64(IEnumerable<HtmlNode> nodes) {
		if (Int64.TryParse(nodes.Single().InnerText, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out Int64 val)) return val;
		return null;
	}

	public static Object? SingleNodeTextToInt64WithSimpleSuffixes(IEnumerable<HtmlNode> nodes) {
		return StringWithSimpleSuffixesToInt64(nodes.Single().InnerText);
	}

	public static Object? SingleAttributeToInt64WithSimpleSuffixes(IEnumerable<String> nodes) {
		return StringWithSimpleSuffixesToInt64(nodes.Single());
	}

	/// <summary>
	/// Tries to parse a <see cref="String"/> to <see cref="Int64"/> like <see cref="long.TryParse(string,out long)"/> but will evaluate si and binary suffixes like k = 1000 and ki = 1024 up to ei
	/// </summary>
	private static Int64? StringWithSimpleSuffixesToInt64(String source) {
		Int32 lastNum = source.LastIndexOfAny(_basicNumberChars);
		ReadOnlySpan<Char> parseMe = source;
		if (lastNum >= 0 && lastNum < source.Length)
			parseMe = parseMe.Slice(0, lastNum + 1);

		if (!Double.TryParse(parseMe, NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out Double doubleVal)) {
			return null;
		}

		Int64 mult = 1;
		if (lastNum >= 0 && lastNum < source.Length) {
			switch (source.Substring(lastNum + 1).ToLowerInvariant().Trim()) {
				case "k":
					mult = 1000;
					break;
				case "ki":
					mult = 1024;
					break;
				case "m":
					mult = 1000000;
					break;
				case "mi":
					mult = 1048576;
					break;
				case "g":
					mult = 1000000000;
					break;
				case "gi":
					mult = 1073741824;
					break;
				case "t":
					mult = 1000000000000;
					break;
				case "ti":
					mult = 1099511627776;
					break;
				case "p":
					mult = 1000000000000000;
					break;
				case "pi":
					mult = 1125899906842624;
					break;
				case "e":
					mult = 1000000000000000000;
					break;
				case "ei":
					mult = 1152921504606846976;
					break;
				default:
					mult = 1;
					break;
			}
		}

		doubleVal *= mult;
		if (doubleVal > Int64.MaxValue || doubleVal < Int64.MinValue) {
			return null;
		}

		return (Int64)doubleVal;
	}

	public static Object? SingleNodeTextShitToInt64(IEnumerable<HtmlNode> nodes) {
		String txt = Regex.Replace(nodes.Single().InnerText, @"[^0-9]", String.Empty);
		if (Int64.TryParse(txt, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out Int64 val)) return val;
		return null;
	}
	
	public static Object? SingleAttributeTextShitToInt64(IEnumerable<String> values) {
		String txt = Regex.Replace(values.Single(), @"[^0-9]", String.Empty);
		if (Int64.TryParse(txt, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out Int64 val)) return val;
		return null;
	}

	public static Object? AtLeastOneNode(IEnumerable<HtmlNode> nodes) => nodes.Any();

	public static Object SingleAttributeHtmlDecoded(IEnumerable<String> values) {
		return HttpUtility.HtmlDecode(values.Single());
	}

	public static Object? SingleAttributeToDateTimeOffset(IEnumerable<String> values) {
		if (DateTimeOffset.TryParse(values.Single(), out DateTimeOffset val)) return val;
		return null;
	}

	public static Object? SingleAttributeToInt64(IEnumerable<String> values) {
		if (Int64.TryParse(values.Single(), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out Int64 val)) return val;
		return null;
	}

	public static Func<IEnumerable<String>, Object?> SingleAttributeContains(String containsMe) {
		return values => values.Single().Contains(containsMe, StringComparison.OrdinalIgnoreCase);
	}

	public static Object? SingleAttributeToUri(IEnumerable<String> values) {
		if (Uri.TryCreate(values.Single(), UriKind.RelativeOrAbsolute, out Uri? val)) return val;
		return null;
	}

	public static HtmlNode ParseAttributeValue(String attributeName, HtmlNode? node) {
		var value = node?.GetAttributeValue(attributeName, null);
		return ParseAttributeValue(value);
	}
	public static HtmlNode ParseAttributeValue(String? html) {
		if(String.IsNullOrWhiteSpace(html)) return HtmlNode.CreateNode(String.Empty);
		string htmlDecode = HttpUtility.HtmlDecode(html);
		return HtmlNode.CreateNode($"<div>{htmlDecode}</div>");
	}
	public static HtmlNode ParseAttributeValue(IEnumerable<String> values) => ParseAttributeValue(values.Single());
}