using BenchmarkDotNet.Running;
using ChessLite.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(PerftBenchmarks).Assembly).Run(args);
