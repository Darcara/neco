# NeCo - Necessary Code - Benchmarks

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/neco.benchmarklibrary)](https://www.nuget.org/packages/Neco.BenchmarkLibrary/)
![GitHub License](https://img.shields.io/github/license/darcara/neco)

A few helpers around [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet)

## Quickstart
```csharp
using Neco.BenchmarkLibrary;

// Run all benchmarks in an assembly with the default (NetConfig) configuration
BenchmarkStarter.Run(typeof(Program).Assembly);

// Run all benchmarks in an assembly with a specific configuation
// noOverwrite: true to save the benchmark in a '[date]-[time]' directory, instead of the default 'results'
BenchmarkStarter.Run<Net8Net9MigrationConfig>(typeof(Program).Assembly, noOverwrite: true);


// Run a specific benchmarks
BenchmarkStarter.Run<SomeBenchmark>();
BenchmarkStarter.Run<SomeBenchmark, Net8Net9MigrationConfig>();
```

## Quick Performance Estimate 

Does not use BenchmarkDotNet, but produces (rough) performance estimates a lot faster (~6 sec per benchmark class).
This supports only a very small subset of Benchmark creation features: GlobalSetup, GlobalCleanup, Params

Results are written to the console
```csharp
using Neco.BenchmarkLibrary;

// Run all benchmarks in an assembly
BenchmarkStarter.QuickBench(typeof(Program).Assembly);

// Run a specific benchmark
BenchmarkStarter.QuickBench<SomeBenchmark>();

// This is almost equivalent to
PerformanceHelper.GetPerformanceRough("SomeBenchmark.Method1", () => new SomeBenchmark().Method());

// Output will look like this
SomeBenchmark.Method1 35,757,090 ops in 5,000.001ms = clean per operation: 0.108µs or 9,298,494.945op/s with 24 Bytes per run and GC 102/0/0
SomeBenchmark.Method1 TotalCPUTime per operation: 4,984.375ms or clean 9,336,433.859op/s for a factor of 0.997

SomeBenchmark.Method2 21,123,454 ops in 5,000.001ms = clean per operation: 0.205µs or 4,873,048.375op/s with 24 Bytes per run and GC 60/0/0
SomeBenchmark.Method2 TotalCPUTime per operation: 5,000.000ms or clean 4,873,049.836op/s for a factor of 1.000

```
