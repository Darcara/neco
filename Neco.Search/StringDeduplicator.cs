namespace Neco.Search;

using System;
using System.Collections.Generic;

public class StringDeduplicator {
	private readonly StringComparer _stringComparer;
	private readonly Dictionary<String, HashSet<String>> _knownWords;

	public StringDeduplicator() : this(StringComparer.Ordinal) {
	}

	public StringDeduplicator(StringComparer stringComparer) {
		_stringComparer = stringComparer;
		_knownWords = new(_stringComparer);
	}

	public Boolean Contains(String word, String? contextWord = null) {
		if (!_knownWords.TryGetValue(word, out HashSet<String>? contextWords)) return false;
		return contextWord == null || contextWords.Contains(contextWord);
	}

	/// <inheritdoc cref="HashSet{T}.Add"/>
	public Boolean Add(String word, String? contextWord = null) {
		if (_knownWords.TryGetValue(word, out HashSet<String>? contextWords)) {
			if (contextWord == null) return false;
			return contextWords.Add(contextWord);
		}

		contextWords = new(_stringComparer);
		if (contextWord != null) contextWords.Add(contextWord);
		_knownWords.Add(word, contextWords);
		return true;
	}
}