namespace Neco.Web.HtmlParsing;

using System;
using System.Linq.Expressions;

public static class Fluent {
	public static INodeSelector Node(String xpath) {
		return new FluentBuilder(xpath);
	}

	public static IAttributeBuilder Attribute(String xpath, String attribute) {
		return new FluentBuilder(xpath).Attribute(attribute);
	}
}

public interface INodeSelector {
	public IAttributeBuilder Attribute(String attribute);
}

public interface IAttributeBuilder {
	public INodeSelector AsHtml();
}

internal sealed class FluentBuilder : INodeSelector, IAttributeBuilder {
	private Expression _expression;
	private readonly FluentBuilder? _parent;
	private readonly String? _xpath;
	private String? _attribute;

	public FluentBuilder(string xpath) {
		_expression = Expression.Parameter(typeof(object), "x");
		_xpath = xpath;
	}

	private FluentBuilder(FluentBuilder parent) {
		_parent = parent;
	}

	#region Implementation of INodeSelector

	/// <inheritdoc />
	public IAttributeBuilder Attribute(String attribute) {
		_attribute = attribute;
		return this;
	}

	#endregion

	#region Implementation of IAttributeBuilder

	/// <inheritdoc />
	public INodeSelector AsHtml() {
		return new FluentBuilder(this);
	}

	#endregion
}