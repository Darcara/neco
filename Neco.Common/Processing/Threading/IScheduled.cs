namespace Neco.Common.Processing.Threading;

/// <summary>
/// A scheduled 
/// </summary>
public interface IScheduled : IDisposable{
	/// <summary>
	/// Some name or identifier for this scheduled function. Should be equivalent to <see cref="object.ToString"/>
	/// </summary>
	public String Name { get; }
}