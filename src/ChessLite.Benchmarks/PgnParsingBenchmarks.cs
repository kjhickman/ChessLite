using BenchmarkDotNet.Attributes;
using ChessLite.Parsing;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
public class PgnParsingBenchmarks
{
    public static IEnumerable<PgnScenario> PgnScenarios =>
    [
        new PgnScenario(
            "ShortMainline",
            "1. e4 e5 2. Nf3 Nc6 *"),
        new PgnScenario(
            "AnnotatedWithVariations",
            "1. e4! {comment} (1. d4 d5) e5 $1 2. Nf3?! Nc6 1/2-1/2"),
        new PgnScenario(
            "FenTagStart",
            "[FEN \"8/8/8/8/8/8/4K3/7k w - - 0 1\"]\n\n1. Kf2 *"),
        new PgnScenario(
            "DisambiguationMainline",
            "1. e4 e5 2. Nf3 Nf6 3. Bc4 Bc5 4. O-O O-O 5. Nc3 a6 6. d3 a5 7. Bd2 a4 8. Qe2 a3 9. Rae1 *"),
    ];

    [ParamsSource(nameof(PgnScenarios))]
    public PgnScenario Scenario { get; set; } = null!;

    public Game Game { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        Game = new Game();
    }

    [Benchmark]
    public Game ParsePgn()
    {
        return PgnParser.Parse(Scenario.Pgn, Game);
    }

    public class PgnScenario(string label, string pgn)
    {
        public string Label { get; } = label;
        public string Pgn { get; } = pgn;

        public override string ToString() => Label;
    }
}
