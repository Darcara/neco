namespace Neco.Common.Processing.Threading;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Neco.Common.ObjectMethodExecutor;

public readonly struct SyncOrAsyncAction : IEquatable<SyncOrAsyncAction> {
	private readonly Object? _taskTarget;
	private readonly MethodInfo _taskMethod;
	private readonly Object?[]? _taskParam;
	private readonly ObjectMethodExecutor _ome;

	[RequiresUnreferencedCode("Calls Neco.Common.ObjectMethodExecutor.ObjectMethodExecutor.Create(MethodInfo, TypeInfo)")]
	[RequiresDynamicCode("Calls Neco.Common.ObjectMethodExecutor.ObjectMethodExecutor.Create(MethodInfo, TypeInfo)")]
	private SyncOrAsyncAction(Object? taskTarget, MethodInfo taskMethod, params Object?[]? taskParams) {
		_taskMethod = taskMethod ?? throw new ArgumentNullException(nameof(taskMethod));
		_taskTarget = taskTarget;
		if (!_taskMethod.IsStatic)
			ArgumentNullException.ThrowIfNull(taskTarget);
		_taskParam = taskParams == null || taskParams.Length == 0 ? null : taskParams;
		_ome = ObjectMethodExecutor.Create(_taskMethod, _taskTarget?.GetType().GetTypeInfo());
	}

	public static SyncOrAsyncAction FromDelegate(Delegate d, params Object?[]? taskParams) {
		ArgumentNullException.ThrowIfNull(d);
		return new SyncOrAsyncAction(d.Target!, d.Method, taskParams);
	}

	public static SyncOrAsyncAction FromMethod(MethodInfo method, Object? target, params Object?[]? taskParams) => new(target, method, taskParams);

	public static SyncOrAsyncAction FromAction(Action a) => new(a.Target, a.Method, null);

	public static SyncOrAsyncAction FromAction<T>(Action<T> a, T param) => new(a.Target, a.Method, param);

	public static SyncOrAsyncAction FromFunc(Func<Task> f) => new(f.Target, f.Method, null);
	public static SyncOrAsyncAction FromFunc<T>(Func<T, Task> f, T param) => new(f.Target, f.Method, param);

	public void Invoke() {
		_ome.Execute(_taskTarget, _taskParam);
	}

	public Boolean IsAsync => _ome.IsMethodAsync;

	public async Task InvokeAsync() {
		await _ome.ExecuteAsync(_taskTarget, _taskParam);
	}

	#region Equality members

	/// <inheritdoc />
	public Boolean Equals(SyncOrAsyncAction other) {
		if (!Equals(_taskTarget, other._taskTarget) || !_taskMethod.Equals(other._taskMethod)) return false;
		if (ReferenceEquals(_taskParam, other._taskParam)) return true;
		if (ReferenceEquals(_taskParam, null)) return false;
		if (ReferenceEquals(other._taskParam, null)) return false;
		if (_taskParam.Length != other._taskParam.Length) return false;
		for (int index = 0; index < _taskParam.Length; index++) {
			Object? myParam = _taskParam[index];
			Object? otherParam = other._taskParam[index];
			if (!Equals(myParam, otherParam)) return false;
		}

		return true;
	}

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => obj is SyncOrAsyncAction other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() {
		Int32 hashcode = HashCode.Combine(_taskTarget, _taskMethod);
		if (_taskParam != null) {
			foreach (var param in _taskParam)
				hashcode = HashCode.Combine(hashcode, param);
		}

		return hashcode;
	}

	public static Boolean operator ==(SyncOrAsyncAction left, SyncOrAsyncAction right) => left.Equals(right);

	public static Boolean operator !=(SyncOrAsyncAction left, SyncOrAsyncAction right) => !left.Equals(right);

	#endregion
}