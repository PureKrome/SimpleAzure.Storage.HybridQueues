using BenchmarkDotNet.Running;
using WorldDomination.SimpleAzure.Storage.HybridQueues.Benchmarks.Benchmarks;

BenchmarkRunner.Run<AddMessageSerializationBenchmarks>();
