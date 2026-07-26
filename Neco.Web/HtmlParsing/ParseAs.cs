namespace Neco.Web.HtmlParsing;

using System;

[Flags]
public enum ParseAs {
	Default = 0,
	/// <summary>
	/// TRUE if the xpath may result in no nodes. <see cref="Parse"/> will return true if optional mappings are not found; FALSE if a node must be found.
	/// </summary>
	Optional = 1,
	StopIfMatched=2,
}