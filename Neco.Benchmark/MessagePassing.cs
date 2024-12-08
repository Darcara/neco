namespace Neco.Benchmark;

using BenchmarkDotNet.Attributes;
using Neco.BenchmarkLibrary.Config;

[Config(typeof(NetConfig))]
public class MessagePassing {
	
}