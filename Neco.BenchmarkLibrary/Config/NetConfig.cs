﻿namespace Neco.BenchmarkLibrary.Config;

using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

/// <summary>
/// Configuration for the latest .Net version. Currently <see cref="CoreRuntime.Core80"/>
/// </summary>
public class NetConfig : BaseConfig {
	public NetConfig() : base(null) {
		AddJob(CreateDefaultJob().WithRuntime(CoreRuntime.Core80).AsDefault());
	}

	
}