namespace Neco.Common.Concurrency;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;
using Neco.Common.Extensions;

internal sealed class StaticObjectBalancer<T> : IObjectBalancer<T> {
	private readonly Channel<T> _cache = Channel.CreateUnbounded<T>();
	private Boolean _isDisposed;

	public StaticObjectBalancer(IEnumerable<T> objects) {
		foreach (T obj in objects)
			_cache.Writer.TryWrite(obj);
		MemoryPool<String>.Shared.Rent();
	}

	#region Implementation of IObjectBalancer<T>

	/// <inheritdoc />
	public T Get() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		if (_cache.Reader.TryRead(out T? cachedObject))
			return cachedObject;

		return _cache.Reader.ReadAsync().GetResultBlocking();
	}

	/// <inheritdoc />
	public ValueTask<T> GetAsync() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		return _cache.Reader.ReadAsync();
	}

	/// <inheritdoc />
	public BalancedObject<T> GetDisposable() => new(this, Get());

	/// <inheritdoc />
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
	public ValueTask<BalancedObject<T>> GetDisposableAsync() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		if (_cache.Reader.TryRead(out T? cachedObject)) {
			return ValueTask.FromResult(new BalancedObject<T>(this, cachedObject));
		}

		return GetDisposableAsyncCore();
	}

	public async ValueTask<BalancedObject<T>> GetDisposableAsyncCore() {
		return new BalancedObject<T>(this, await _cache.Reader.ReadAsync().ConfigureAwait(false));
	}

	/// <inheritdoc />
	public void Return(T obj) {
		if (_isDisposed) {
			DisposeCachedObject(obj);
			return;
		}

		if (!_cache.Writer.TryWrite(obj))
			DisposeCachedObject(obj);
	}

	#endregion

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		if(_isDisposed) return;
		_isDisposed = true;
		_cache.Writer.Complete();

		while (_cache.Reader.TryRead(out T? cachedObject)) {
			DisposeCachedObject(cachedObject);
		}
	}

	private static void DisposeCachedObject(T t) {
		if (t is IDisposable disposable)
			disposable.Dispose();
	}

	#endregion
}