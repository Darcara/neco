namespace Neco.BenchmarkLibrary.Config;

using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

public class Net8Net9MigrationConfig : NetConfig {
	/// <inheritdoc />
	public Net8Net9MigrationConfig() {
		// specify 8.0 explicitly so if it is the default job, it will not be overridden by 9.0
		AddJob(CreateDefaultJob().WithRuntime(CoreRuntime.Core80));
		AddJob(CreateDefaultJob().WithRuntime(CoreRuntime.Core90));
	}
}