using BenchmarkDotNet.Attributes;
using ChessLite.Parsing;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 25_000)]
public class PgnWritingBenchmarks
{
    public static IEnumerable<PgnWriteScenario> PgnWriteScenarios =>
    [
        new PgnWriteScenario(
            "ShortMainline",
            ["e2e4", "e7e5", "g1f3", "b8c6"]),
        new PgnWriteScenario(
            "Castle",
            ["g1f3", "g8f6", "g2g3", "g7g6", "f1g2", "f8g7", "e1g1"]),
        new PgnWriteScenario(
            "Checkmate",
            ["f2f3", "e7e5", "g2g4", "d8h4"]),
        new PgnWriteScenario(
            "Promotion",
            ["a2a4", "h7h5", "a4a5", "h5h4", "a5a6", "h4h3", "a6b7", "h3g2", "b7a8q"]),
        new PgnWriteScenario(
            "DisambiguationMainline",
            ["e2e4", "e7e5", "g1f3", "g8f6", "f1c4", "f8c5", "e1g1", "e8g8", "b1c3", "a7a6", "d2d3", "a6a5", "c1d2", "a5a4", "d1e2", "a4a3", "a1e1"]),
    ];

    [ParamsSource(nameof(PgnWriteScenarios))]
    public PgnWriteScenario Scenario { get; set; } = null!;

    public Game Game { get; set; } = null!;

    [IterationSetup]
    public void Setup()
    {
        Game = new Game();
        foreach (var move in Scenario.UciMoves)
        {
            Game.MakeUciMove(move);
        }
    }

    [Benchmark]
    public string WritePgn()
    {
        return Pgn.Format(Game);
    }

    public class PgnWriteScenario(string label, string[] uciMoves)
    {
        public string Label { get; } = label;
        public string[] UciMoves { get; } = uciMoves;

        public override string ToString() => Label;
    }
}
