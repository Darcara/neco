namespace Neco.BenchmarkLibrary.Config;

using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

public class Net8Net9MigrationConfig : BaseConfig {
	/// <inheritdoc />
	public Net8Net9MigrationConfig() : base(null){
		AddJob(CreateDefaultJob().WithRuntime(CoreRuntime.Core80));
		AddJob(CreateDefaultJob().WithRuntime(CoreRuntime.Core90));
	}
}