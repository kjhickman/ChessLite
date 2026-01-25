using BenchmarkDotNet.Attributes;
using ChessLite.Parsing;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
public class FenParsingBenchmarks
{
    public static IEnumerable<FenScenario> FenScenarios =>
    [
        new FenScenario(
            "StartingPosition",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"),
        new FenScenario(
            "ComplexMiddlegame",
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"),
        new FenScenario(
            "SimpleEndgame",
            "8/8/1k6/6N1/1pp3P1/1P3K1p/8/8 w - - 0 9"),
        new FenScenario(
            "WithEnPassant",
            "rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3"),
        new FenScenario(
            "HighHalfmoveClock",
            "8/5k2/3p4/1p1Pp2p/pP2Pp1P/P4P1K/8/8 b - - 99 50"),
    ];

    [ParamsSource(nameof(FenScenarios))]
    public FenScenario Scenario { get; set; } = null!;

    public Position Position { get; set; } = null!;

    [IterationSetup]
    public void Setup()
    {
        Position = new Position { Mailbox = new PieceType[64] };;
    }

    [Benchmark]
    public bool ParseFen()
    {
        return FenParser.Parse(Scenario.Fen, Position);
    }

    public class FenScenario(string label, string fen)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;

        public override string ToString() => Label;
    }
}
