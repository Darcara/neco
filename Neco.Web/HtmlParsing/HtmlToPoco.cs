namespace Neco.Web.HtmlParsing;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HtmlAgilityPack;
using Neco.Common.Extensions;

public interface IHtmlToPoco {
	Boolean Parse(HtmlNode root, Object poco, IParseLog? log = null);
}

public interface IHtmlToPoco<TPoco> : IHtmlToPoco {
	Boolean TryParse(HtmlNode root, [MaybeNullWhen(false)] out TPoco t);
}

/// <summary>
/// 
/// </summary>
/// <remarks>Mappings will be evaluated in the order they are registered</remarks>
public class HtmlToPoco<TPoco> : IHtmlToPoco<TPoco>, IEnumerable<XPathMapping> where TPoco : class, new() {
	private readonly List<XPathMapping> _mappings = new();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="xpath"></param>
	/// <param name="memberName"></param>
	/// <param name="converterFunc">The function to convert the <seealso cref="HtmlNodeCollection"/> to the type of <see cref="memberName"/>. If null, the (<seealso cref="String"/>)<seealso cref="HtmlNode.InnerText"/> of the first result is used.</param>
	/// <param name="parentXPath">The parent node that the <see cref="xpath"/> is relative to</param>
	/// <param name="parseOptions">...</param>
	public void RegisterNodeMapping(String xpath, String memberName, Func<IEnumerable<HtmlNode>, Object?>? converterFunc = null, String? parentXPath = null, ParseAs parseOptions = ParseAs.Default) {
		Add(CreateMapping(m => new XPathNodeMapping(xpath, m, parentXPath, parseOptions, converterFunc), memberName));
	}

	public void RegisterAttributeMapping(String xpath, String propertyName, String memberName, Func<IEnumerable<String>, Object?>? converterFunc = null, String? parentXPath = null, ParseAs parseOptions = ParseAs.Default) {
		Add(CreateMapping(m => new XPathAttributeMapping(xpath, m, parentXPath, parseOptions, converterFunc, propertyName), memberName));
	}

	public Boolean TryParse(String htmlString, [MaybeNullWhen(false)] out TPoco t) => TryParse(htmlString, null, out t);

	public Boolean TryParse(String htmlString, IParseLog? log, [MaybeNullWhen(false)] out TPoco t) {
		HtmlDocument html = new();
		html.LoadHtml(htmlString);
		return TryParse(html.DocumentNode, log, out t);
	}

	public Boolean TryParse(HtmlNode root, [MaybeNullWhen(false)] out TPoco t) => TryParse(root, null, out t);

	public Boolean TryParse(HtmlNode root, IParseLog? log, [MaybeNullWhen(false)] out TPoco t) {
		TPoco newT = new();
		Boolean result = Parse(root, newT, log);
		if (result) {
			t = newT;
			return true;
		}

		t = default(TPoco);
		return false;
	}

	public Boolean Parse(String htmlString, Object poco, IParseLog? log = null) {
		HtmlDocument html = new();
		html.LoadHtml(htmlString);
		return Parse(html.DocumentNode, poco, log);
	}

	public Boolean Parse(HtmlNode root, Object poco, IParseLog? log = null) {
		ArgumentNullException.ThrowIfNull(root);
		ArgumentNullException.ThrowIfNull(poco);
		log ??= NoLog.Instance;

		Boolean result = true;
		foreach (XPathMapping mapping in _mappings) {
			PropertyInfo prop = poco.GetType().GetProperty(mapping.MemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			if (prop == null || !prop.CanWrite) {
				log.Error($"Type {poco.GetType().FullName} does not have a member named {mapping.MemberName} or it is not public and writeable");
				result = false;
				continue;
			}

			HtmlNodeCollection xpathResult;
			xpathResult = root.SelectNodes(mapping.XPath);

			if (xpathResult == null || xpathResult.Count == 0) {
				if (!mapping.ParseOptions.HasFlag(ParseAs.Optional)) {
					result = false;
					log.Warn($"Failed to evaluate xpath {mapping.XPath.Expression} --> {poco.GetType().Name}.{mapping.MemberName}");
				}

				log.Trace($"XPath({mapping.XPath.Expression}) found 0 result(s) -- marked optional");
				continue;
			}

			log.Trace($"XPath({mapping.XPath.Expression}) found {xpathResult.Count} result(s)");

			try {
				if (mapping.TryMap(log, prop, poco, xpathResult, out Object? val)) {
					try {
						prop.SetValue(poco, val, null);
					}
					catch (Exception e) {
						log.Error($"Unable to set value for xpath {mapping.XPath.Expression} --> [{prop.PropertyType.GetGenericName()}]{poco.GetType().Name}.{mapping.MemberName} = [{val?.GetType().GetGenericName()}]{val}{Environment.NewLine}{e}");
					}
					log.Trace($"xpath {mapping.XPath.Expression} --> {poco.GetType().Name}.{mapping.MemberName} = {val}");
					
				} else {
					log.Error($"Unable to map xpath {mapping.XPath.Expression} --> {poco.GetType().Name}.{mapping.MemberName}");
					return result;
				}
			}
			catch (Exception e) {
				log.Error($"Error while evaluating {mapping.XPath.Expression} --> {poco.GetType().Name}.{mapping.MemberName}{Environment.NewLine}{e}");
			}

			if (mapping.ParseOptions.HasFlag(ParseAs.StopIfMatched))
				return result;
		}

		return result;
	}

	public void Add((String xPath, String memberName, Func<IEnumerable<HtmlNode>, Object?>? converterFunc) mapping) {
		RegisterNodeMapping(mapping.xPath, mapping.memberName, mapping.converterFunc);
	}

	public void Add((String xPath, String memberName, Func<IEnumerable<HtmlNode>, Object?>? converterFunc, ParseAs parseOptions) mapping) {
		RegisterNodeMapping(mapping.xPath, mapping.memberName, mapping.converterFunc, null, mapping.parseOptions);
	}

	public void Add((String xPath, String memberName, Func<IEnumerable<HtmlNode>, Object?>? converterFunc, String? parentXPath, ParseAs parseOptions) mapping) {
		RegisterNodeMapping(mapping.xPath, mapping.memberName, mapping.converterFunc, mapping.parentXPath, mapping.parseOptions);
	}

	public void Add((String xPath, String propertyName, String memberName) mapping) {
		RegisterAttributeMapping(mapping.xPath, mapping.propertyName, mapping.memberName);
	}

	public void Add((String xPath, String propertyName, String memberName, Func<IEnumerable<String>, Object?>? converterFunc) mapping) {
		RegisterAttributeMapping(mapping.xPath, mapping.propertyName, mapping.memberName, mapping.converterFunc);
	}

	public void Add((String xPath, String propertyName, String memberName, Func<IEnumerable<String>, Object?>? converterFunc, ParseAs parseOptions) mapping) {
		RegisterAttributeMapping(mapping.xPath, mapping.propertyName, mapping.memberName, mapping.converterFunc, null, mapping.parseOptions);
	}

	public void Add((String xPath, String memberName, IHtmlToPoco mapping) mapping) {
		Add(CreateMapping(m => new XPathCollectionMapping(mapping.xPath, m, ParseAs.Default, mapping.mapping, null), mapping.memberName));
	}

	public void Add((String xPath, String memberName, IHtmlToPoco mapping, ParseAs parseOptions) mapping) {
		Add(CreateMapping(m => new XPathCollectionMapping(mapping.xPath, m,mapping.parseOptions, mapping.mapping, null), mapping.memberName));
	}
	
	public void Add((String xPath, String attributeName, Func<String, HtmlNode> converterFunc, String subxPath, String memberName, IHtmlToPoco mapping, ParseAs parseOptions) mapping) {
		Add(CreateMapping(m => new XPathCollectionMapping2(mapping.xPath, mapping.attributeName, mapping.converterFunc, mapping.subxPath, m, mapping.parseOptions, mapping.mapping), mapping.memberName));
	}

	public void Add((String memberName, Object value) mapping) {
		Add(CreateMapping(m => new ConstantMapping(m, mapping.value), mapping.memberName));
	}

	public void Add((String memberName, Func<Object> valueGenerator) mapping) {
		Add(CreateMapping(m => new GeneratedValueMapping(m, mapping.valueGenerator), mapping.memberName));
	}

	private static TMapping CreateMapping<TMapping>(Func<String, TMapping> mappingCreator, string memberName) {
		try {
			return mappingCreator(memberName);
		}
		catch (Exception e) {
			throw new Exception($"Unable to create {typeof(TMapping).GetGenericName()} for {typeof(TPoco).GetGenericName()}.{memberName}", e);
		}
	}

	public void Add(XPathMapping mapping) {
		_mappings.Add(mapping);
	}

	public void Add(IEnumerable<XPathMapping> mapping) {
		_mappings.AddRange(mapping);
	}

	#region Implementation of IEnumerable

	/// <inheritdoc />
	public IEnumerator<XPathMapping> GetEnumerator() => _mappings.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion
}