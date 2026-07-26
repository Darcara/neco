namespace Neco.Web.HtmlParsing;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.XPath;
using HtmlAgilityPack;
using Neco.Common.Extensions;

public interface IStringMapping {
}

public interface IDocumentMapping {
}

public abstract class XPathMapping {
	public readonly XPathExpression XPath;
	public readonly String MemberName;
	public readonly ParseAs ParseOptions;

	protected XPathMapping(String xPath, String memberName, ParseAs parseOptions) {
		MemberName = memberName;
		ParseOptions = parseOptions;
		XPath = XPathExpression.Compile(xPath);
	}

	public abstract Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue);
}

public class XPathNodeMapping : XPathMapping {
	public readonly Func<IEnumerable<HtmlNode>, Object?>? Converter;

	/// <inheritdoc />
	public XPathNodeMapping(String xPath, String memberName, String? parentXPath, ParseAs parseOptions, Func<IEnumerable<HtmlNode>, Object?>? converter) : base(xPath, memberName, parseOptions) {
		Converter = converter;
	}

	#region Overrides of XPathMapping

	/// <inheritdoc />
	public override Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue) {
		var val = xpathResult.Single().InnerText;
		if (Converter != null)
			propertyValue = Converter(xpathResult);
		else
			propertyValue = val;
		return true;
	}

	#endregion
}

public class XPathCollectionMapping : XPathMapping {
	private readonly IHtmlToPoco _subMapping;
	private readonly Func<HtmlNode, HtmlNode>? _converter;

	/// <inheritdoc />
	public XPathCollectionMapping(String xPath, String memberName, ParseAs parseOptions, IHtmlToPoco subMapping, Func<HtmlNode, HtmlNode>? converter) : base(xPath, memberName, parseOptions) {
		_subMapping = subMapping;
		_converter = converter;
	}

	private bool Parse(HtmlNode htmlNode, object collectionItem, IParseLog? log = null) {
		HtmlNode node = htmlNode;
		if (_converter != null)
			node = _converter(node);
		return _subMapping.Parse(node, collectionItem, log);
	}

	#region Overrides of XPathMapping

	/// <inheritdoc />
	public override Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue) {
		ArgumentNullException.ThrowIfNull(log);
		ArgumentNullException.ThrowIfNull(prop);
		ArgumentNullException.ThrowIfNull(poco);
		ArgumentNullException.ThrowIfNull(xpathResult);

		propertyValue = null;
		if (!prop.PropertyType.IsGenericType || prop.PropertyType.GenericTypeArguments.Length != 1) {
			log.Warn($"Unable to identify type of collection-item. Should be List<T> --> {poco.GetType().Name}.{MemberName}");
			return false;
		}

		IList? values;
		Type collectionItemType = prop.PropertyType.GenericTypeArguments.Single();
		IList? availableCollection = prop.GetValue(poco) as IList;
		if (availableCollection != null) {
			values = availableCollection;
		} else {
			values = Activator.CreateInstance(prop.PropertyType) as IList;
			if (values == null) {
				log.Warn($"Unable to instantiate collection ({prop.PropertyType.GetFullGenericName()}) --> {poco.GetType().Name}.{MemberName}");
				return false;
			}
		}

		foreach (HtmlNode htmlNode in xpathResult) {
			Object? collectionItem = Activator.CreateInstance(collectionItemType);
			if (collectionItem == null) {
				log.Warn($"Unable to instantiate collection-item ({collectionItemType.FullName}) --> {poco.GetType().Name}.{MemberName}");
				return false;
			}

			if (!Parse(htmlNode, collectionItem, log)) {
				if (ParseOptions.HasFlag(ParseAs.Optional)) {
					log.Warn($"Failed to evaluate optional xpath {XPath.Expression} --> {poco.GetType().Name}.{MemberName}");
				} else {
					log.Error($"Failed to evaluate xpath {XPath.Expression} --> {poco.GetType().Name}.{MemberName}");
					return false;
				}

				continue;
			}

			values.Add(collectionItem);
		}

		propertyValue = values;
		return true;
	}

	#endregion
}

public class XPathCollectionMapping2 : XPathMapping {
	private readonly String _attributeName;
	private readonly IHtmlToPoco _subMapping;
	private readonly Func<String, HtmlNode> _converter;
	private readonly XPathExpression _subxpath;

	/// <inheritdoc />
	public XPathCollectionMapping2(String xPath, String attributeName, Func<String, HtmlNode> converter, String subxpath, String memberName, ParseAs parseOptions, IHtmlToPoco subMapping) : base(xPath, memberName, parseOptions) {
		_attributeName = attributeName;
		_subMapping = subMapping;
		_converter = converter;
		_subxpath = XPathExpression.Compile(subxpath);
	}

	#region Overrides of XPathMapping

	/// <inheritdoc />
	public override Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue) {
		ArgumentNullException.ThrowIfNull(log);
		ArgumentNullException.ThrowIfNull(prop);
		ArgumentNullException.ThrowIfNull(poco);
		ArgumentNullException.ThrowIfNull(xpathResult);

		propertyValue = null;
		if (!prop.PropertyType.IsGenericType || prop.PropertyType.GenericTypeArguments.Length != 1) {
			log.Warn($"Unable to identify type of collection-item. Should be List<T> --> {poco.GetType().Name}.{MemberName}");
			return false;
		}

		IList? values;
		Type collectionItemType = prop.PropertyType.GenericTypeArguments.Single();
		IList? availableCollection = prop.GetValue(poco) as IList;
		if (availableCollection != null) {
			values = availableCollection;
		} else {
			values = Activator.CreateInstance(prop.PropertyType) as IList;
			if (values == null) {
				log.Warn($"Unable to instantiate collection ({prop.PropertyType.GetFullGenericName()}) --> {poco.GetType().Name}.{MemberName}");
				return false;
			}
		}

		foreach (HtmlNode htmlNode in xpathResult) {
			var attrValue = htmlNode.GetAttributeValue(_attributeName, null);
			if (String.IsNullOrWhiteSpace(attrValue)) continue;
			var node = _converter(attrValue);
			HtmlNodeCollection htmlNodeCollection = node.SelectNodes(_subxpath);
			log.Trace($"XPath-sub ({_subxpath.Expression}) in '{XPath.Expression}' found {htmlNodeCollection?.Count ?? 0} results");
			if (htmlNodeCollection == null || htmlNodeCollection.Count == 0)
				continue;
			
			foreach (HtmlNode subNode in htmlNodeCollection) {
				Object? collectionItem = Activator.CreateInstance(collectionItemType);
				if (collectionItem == null) {
					log.Warn($"Unable to instantiate collection-item ({collectionItemType.FullName}) --> {poco.GetType().Name}.{MemberName}");
					return false;
				}


				if (!_subMapping.Parse(subNode, collectionItem, log)) {
					if (ParseOptions.HasFlag(ParseAs.Optional)) {
						log.Warn($"Failed to evaluate optional xpath {XPath.Expression} --> {poco.GetType().Name}.{MemberName}");
					} else {
						log.Error($"Failed to evaluate xpath {XPath.Expression} --> {poco.GetType().Name}.{MemberName}");
						return false;
					}

					continue;
				}

				values.Add(collectionItem);
			}
		}

		propertyValue = values;
		return true;
	}

	#endregion
}

public class XPathAttributeMapping : XPathMapping {
	private readonly Func<IEnumerable<String>, Object?>? _converter;
	private readonly String _attributeName;

	/// <inheritdoc />
	public XPathAttributeMapping(String xPath, String memberName, String? parentXPath, ParseAs parseOptions, Func<IEnumerable<String>, Object?>? converter, String attributeName) : base(xPath, memberName, parseOptions) {
		_converter = converter;
		_attributeName = attributeName;
	}

	#region Overrides of XPathMapping

	/// <inheritdoc />
	public override Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue) {
		IEnumerable<String> attributeValues = xpathResult.Select(n => n.GetAttributeValue(_attributeName, null)).WhereNotNull();

		if (_converter != null)
			propertyValue = _converter(attributeValues);
		else
			propertyValue = attributeValues.Single();
		return true;
	}

	#endregion
}

public class ConstantMapping : XPathMapping {
	private readonly Object _value;

	/// <inheritdoc />
	public ConstantMapping(String memberName, Object value) : base(".", memberName, ParseAs.Default) {
		_value = value;
	}

	#region Overrides of XPathMapping

	/// <inheritdoc />
	public override Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue) {
		propertyValue = _value;
		return true;
	}

	#endregion
}

public class GeneratedValueMapping : XPathMapping {
	private readonly Func<Object?> _valueGenerator;

	/// <inheritdoc />
	public GeneratedValueMapping(String memberName, Func<Object?> valueGenerator) : base(".", memberName, ParseAs.Default) {
		_valueGenerator = valueGenerator;
	}

	#region Overrides of XPathMapping

	/// <inheritdoc />
	public override Boolean TryMap(IParseLog log, PropertyInfo prop, Object poco, IEnumerable<HtmlNode> xpathResult, out Object? propertyValue) {
		propertyValue = _valueGenerator();
		return true;
	}

	#endregion
}