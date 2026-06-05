using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using ChessLite.Benchmarks;

var config = ManualConfig.Create(DefaultConfig.Instance);
config.AddJob(Job.Default.WithToolchain(ChessLiteBenchmarkToolchain.Instance));

BenchmarkSwitcher.FromAssembly(typeof(PerftBenchmarks).Assembly).Run(args, config);

internal static class ChessLiteBenchmarkToolchain
{
    internal static readonly IToolchain Instance = new Toolchain(
        nameof(ChessLiteBenchmarkToolchain),
        new ChessLiteBenchmarkGenerator(),
        new DotNetCliBuilder(NetCoreAppSettings.NetCoreApp10_0.TargetFrameworkMoniker, customDotNetCliPath: null!),
        new DotNetCliExecutor(customDotNetCliPath: null!));
}

internal sealed class ChessLiteBenchmarkGenerator : CsProjGenerator
{
    private static readonly FileInfo ProjectFile = new(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../ChessLite.Benchmarks.csproj")));

    internal ChessLiteBenchmarkGenerator()
        : base(NetCoreAppSettings.NetCoreApp10_0.TargetFrameworkMoniker, cliPath: null!, packagesPath: null!, runtimeFrameworkVersion: null!)
    {
    }

    protected override FileInfo GetProjectFilePath(Type benchmarkTarget, ILogger logger)
    {
        return ProjectFile;
    }
}
