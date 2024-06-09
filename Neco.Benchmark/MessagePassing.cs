namespace Neco.Benchmark;

using BenchmarkDotNet.Attributes;
using Neco.Benchmark.Config;

[Config(typeof(NetCoreConfig))]
public class MessagePassing {
	
}