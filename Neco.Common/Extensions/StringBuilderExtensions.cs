namespace Neco.Common.Extensions;

using System;
using System.Text;

public static class StringBuilderExtensions {

	public static StringBuilder AppendUpperInvariant(this StringBuilder builder, String? value) {
		if (String.IsNullOrEmpty(value)) return builder;

		builder.EnsureCapacity(builder.Length + value.Length);
		builder.Append(value.ToUpperInvariant());

		return builder;
	}

	public static StringBuilder AppendLowerInvariant(this StringBuilder builder, String? value) {
		if (String.IsNullOrEmpty(value)) return builder;

		builder.EnsureCapacity(builder.Length + value.Length);
		builder.Append(value.ToLowerInvariant());

		return builder;
	}
}