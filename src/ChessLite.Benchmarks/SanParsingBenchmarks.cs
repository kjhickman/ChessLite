using BenchmarkDotNet.Attributes;
using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.State;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 750_000)]
public class SanParsingBenchmarks
{
    public static IEnumerable<SanScenario> SanScenarios =>
    [
        new SanScenario(
            "PawnQuiet",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "e4"),
        new SanScenario(
            "KnightMove",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "Nf3"),
        new SanScenario(
            "Capture",
            "r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3",
            "Nxe5"),
        new SanScenario(
            "Castle",
            "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4",
            "O-O"),
        new SanScenario(
            "Check",
            "r1bqkbnr/pppp1ppp/2n5/4p2Q/4P3/8/PPPP1PPP/RNB1KBNR w KQkq - 2 3",
            "Qxf7+"),
        new SanScenario(
            "Promotion",
            "8/P6k/8/8/8/8/8/6K1 w - - 0 1",
            "a8=Q"),
        new SanScenario(
            "DisambiguationByRank",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "N1c3",
            ["g1f3", "a7a6", "f3d4", "a6a5", "d4b5", "h7h6"]),
    ];

    [ParamsSource(nameof(SanScenarios))]
    public SanScenario Scenario { get; set; } = null!;

    public Position Position { get; set; } = null!;

    [IterationSetup]
    public void Setup()
    {
        var game = new Game(Fen.Parse(Scenario.Fen));
        if (Scenario.UciMoves.Length > 0)
        {
            foreach (var move in Scenario.UciMoves)
            {
                game.MakeUciMove(move);
            }
        }

        Position = game.Position.Clone();
    }

    [Benchmark]
    public Move ParseSan()
    {
        return SanParser.MatchMove(Position, Scenario.San);
    }

    public class SanScenario(string label, string fen, string san, string[]? uciMoves = null)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;
        public string San { get; } = san;
        public string[] UciMoves { get; } = uciMoves ?? Array.Empty<string>();

        public override string ToString() => Label;
    }
}
