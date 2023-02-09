namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

public interface IActionQueue : IDisposable {
	public Task WaitUntilEmpty();
	public void Enqueue(Action doMe);

	public void Enqueue(Func<Task> doMe);

	public void Enqueue<TArg1>(Action<TArg1> doMe, TArg1 arg1);

	public void Enqueue<TArg1>(Func<TArg1, Task> doMe, TArg1 arg1);

	public void Enqueue<TArg1, TArg2>(Action<TArg1, TArg2> doMe, TArg1 arg1, TArg2 arg2);

	public void Enqueue<TArg1, TArg2>(Func<TArg1, TArg2, Task> doMe, TArg1 arg1, TArg2 arg2);
}