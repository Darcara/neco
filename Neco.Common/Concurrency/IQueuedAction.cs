namespace Neco.Common.Concurrency;

using System.Threading.Tasks;

public interface IQueuedAction {
	public Task InvokeAsync();
	// TODO string name & string type for better logging ?
}