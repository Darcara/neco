namespace Neco.Common.Extensions;

using System;
using System.Text;

public static class StringBuilderExtensions {
	public static StringBuilder AppendLineFormat(this StringBuilder source, String format, params Object[] p) {
		source.AppendFormat(format, p);
		source.Append(Environment.NewLine);
		return source;
	}

	public static StringBuilder AppendUpperInvariant(this StringBuilder builder, String? value) {
		if (String.IsNullOrEmpty(value)) return builder;

		builder.EnsureCapacity(builder.Length + value.Length);
		for (var i = 0; i < value.Length; i++) {
			builder.Append(Char.ToUpperInvariant(value[i]));
		}

		return builder;
	}

	public static StringBuilder AppendLowerInvariant(this StringBuilder builder, String? value) {
		if (String.IsNullOrEmpty(value)) return builder;

		builder.EnsureCapacity(builder.Length + value.Length);
		for (var i = 0; i < value.Length; i++) {
			builder.Append(Char.ToLowerInvariant(value[i]));
		}

		return builder;
	}
}