namespace Neco.Search;

using System;
using System.Collections.Generic;

internal interface IStringStorage<TId> {
	void Add(String word, Int32 score, TId id, String? keyword);
}

internal class StoreByWord<TId> :IStringStorage<TId> {
	private Dictionary<String, TId> _data;
	
	#region Implementation of IStringStorage<TId>

	/// <inheritdoc />
	public void Add(String word, Int32 score, TId id, String? keyword) {
		//[keyword][word][score] = [ids]
		//[word][score] = [ids]
	}

	#endregion
}

internal class StoreByScore<TId> :IStringStorage<TId>{
	#region Implementation of IStringStorage<TId>

	/// <inheritdoc />
	public void Add(String word, Int32 score, TId id, String? keyword) {
		// [score][keyword][word] = [ids]
		// [score][word] = [ids]
	}

	#endregion
}