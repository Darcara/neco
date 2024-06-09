namespace Neco.Search;

using System;
using System.Collections.Generic;

public interface IEncoder {
	public String Encode(String input);
}
public sealed class IdentityEncoder : IEncoder {
	#region Implementation of IEncoder

	/// <inheritdoc />
	public String Encode(String input) => input;

	#endregion
}
public sealed class CaseInsensitiveEncoder : IEncoder {
	#region Implementation of IEncoder

	/// <inheritdoc />
	public String Encode(String input) => input.ToUpperInvariant();

	#endregion
}

public interface IEncoderPipeline : IEncoder {
	public IReadOnlyList<IEncoder> Encoders { get; }
	public Int32 Count { get; }
	public void Add(IEncoder item);
	public void AddRange(IEnumerable<IEncoder> collection);
	public void Clear();
}

public class DefaultEncoderPipeline : IEncoderPipeline {
	private readonly List<IEncoder> _encoders = new();

	#region Implementation of IEncoder

	/// <inheritdoc />
	public String Encode(String input) {
		String output = input;
		foreach (IEncoder encoder in _encoders) {
			output = encoder.Encode(output);
		}

		return output;
	}

	#endregion

	#region Implementation of IEncoderPipeline

	public void Add(IEncoder item) => _encoders.Add(item);

	public void AddRange(IEnumerable<IEncoder> collection) => _encoders.AddRange(collection);

	public void Clear() => _encoders.Clear();

	public Int32 Count => _encoders.Count;

	/// <inheritdoc />
	public IReadOnlyList<IEncoder> Encoders => _encoders.AsReadOnly();

	#endregion
}