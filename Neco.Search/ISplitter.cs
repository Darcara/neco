namespace Neco.Search;

using System;
using System.Linq;
using System.Text.RegularExpressions;

public interface ISplitter {
	public String[] Split(String input);
}

public sealed partial class SimpleWhitespaceSplitter : ISplitter {
	#region Implementation of ITokenizer

	/// <inheritdoc />
	public String[] Split(String input) => WhitespaceRegex().Split(input).Where(t => !String.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray();

	[GeneratedRegex(@"[\s\t\r\n\f\u0085\u2028\u2029]+")]
	private static partial Regex WhitespaceRegex();

	#endregion
}