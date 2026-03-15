using BenchmarkDotNet.Attributes;
using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.State;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 15_000_000)]
public class UciParsingBenchmarks
{
    public static IEnumerable<UciScenario> UciScenarios =>
    [
        new UciScenario(
            "QuietMove",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "e2e4"),
        new UciScenario(
            "Capture",
            "r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3",
            "f3e5"),
        new UciScenario(
            "ShortCastle",
            "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4",
            "e1g1"),
        new UciScenario(
            "EnPassant",
            "rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2",
            "e5d6"),
        new UciScenario(
            "Promotion",
            "8/P6k/8/8/8/8/8/6K1 w - - 0 1",
            "a7a8q"),
    ];

    [ParamsSource(nameof(UciScenarios))]
    public UciScenario Scenario { get; set; } = null!;

    public Position Position { get; set; } = null!;

    [IterationSetup]
    public void Setup()
    {
        Position = Fen.Parse(Scenario.Fen);
    }

    [Benchmark]
    public Move ParseUci()
    {
        return UciParser.MoveFromUci(Position, Scenario.Uci);
    }

    public class UciScenario(string label, string fen, string uci)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;
        public string Uci { get; } = uci;

        public override string ToString() => Label;
    }
}
