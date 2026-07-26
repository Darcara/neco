namespace Neco.Common.Processing.Threading;

using System.Threading.Tasks;

public interface ITaskRunner : IDisposable {
	public void EnqueueTask(SyncOrAsyncAction task);
	public void EnqueueTask(Action task);
	public void EnqueueTask<T1>(Action<T1> task, T1 t1);
	public void EnqueueTask<T1, T2>(Action<T1, T2> task, T1 t1, T2 t2);
	public void EnqueueTask<T1, T2, T3>(Action<T1, T2, T3> task, T1 t1, T2 t2, T3 t3);
	public void EnqueueTask(Func<Task> task);
	public void EnqueueTask<T1>(Func<T1, Task> task, T1 t1);
	public void EnqueueTask<T1, T2>(Func<T1, T2, Task> task, T1 t1, T2 t2);
	public void EnqueueTask<T1, T2, T3>(Func<T1, T2, T3, Task> task, T1 t1, T2 t2, T3 t3);
}

public sealed class ThreadPoolExecutor : ITaskRunner {
	#region Implementation of IDisposable

	/// <inheritdoc />
	public void Dispose() {
	}

	#endregion

	#region Implementation of ITaskRunner

	/// <inheritdoc />
	public void EnqueueTask(SyncOrAsyncAction task) {
		if (task.IsAsync)
			Task.Run(task.InvokeAsync);
		else
			Task.Run(task.Invoke);
	}

	/// <inheritdoc />
	public void EnqueueTask(Action task) => Task.Run(task);

	/// <inheritdoc />
	public void EnqueueTask<T1>(Action<T1> task, T1 t1) => Task.Run(() => task(t1));

	/// <inheritdoc />
	public void EnqueueTask<T1, T2>(Action<T1, T2> task, T1 t1, T2 t2) => Task.Run(() => task(t1, t2));

	/// <inheritdoc />
	public void EnqueueTask<T1, T2, T3>(Action<T1, T2, T3> task, T1 t1, T2 t2, T3 t3)  => Task.Run(() => task(t1, t2, t3));

	/// <inheritdoc />
	public void EnqueueTask(Func<Task> task) => Task.Run(task);

	/// <inheritdoc />
	public void EnqueueTask<T1>(Func<T1, Task> task, T1 t1)  => Task.Run(() => task(t1));

	/// <inheritdoc />
	public void EnqueueTask<T1, T2>(Func<T1, T2, Task> task, T1 t1, T2 t2)  => Task.Run(() => task(t1, t2));

	/// <inheritdoc />
	public void EnqueueTask<T1, T2, T3>(Func<T1, T2, T3, Task> task, T1 t1, T2 t2, T3 t3)  => Task.Run(() => task(t1, t2, t3));

	#endregion
}
