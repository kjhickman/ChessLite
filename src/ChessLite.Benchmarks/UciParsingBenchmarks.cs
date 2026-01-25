using BenchmarkDotNet.Attributes;
using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.State;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 20_000_000)]
public class UciParsingBenchmarks
{
    public static IEnumerable<UciMoveScenario> UciMoveScenarios =>
    [
        new UciMoveScenario(
            "RegularMove",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "e2e4"),
        new UciMoveScenario(
            "CaptureMove",
            "rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2",
            "e4d5"),
        new UciMoveScenario(
            "KingsideCastle",
            "rnbqkbnr/pppppppp/8/8/8/5N2/PPPPPPPP/RNBQKB1R w KQkq - 2 2",
            "e1g1"),
        new UciMoveScenario(
            "QueensideCastle",
            "rnbqkbnr/pppppppp/8/8/8/2N5/PPPPPPPP/R1BQKBNR w KQkq - 2 2",
            "e1c1"),
        new UciMoveScenario(
            "EnPassant",
            "rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3",
            "e5f6"),
        new UciMoveScenario(
            "PromotionToQueen",
            "4k3/P7/8/8/8/8/8/4K3 w - - 0 1",
            "a7a8q"),
        new UciMoveScenario(
            "PromotionToKnight",
            "4k3/P7/8/8/8/8/8/4K3 w - - 0 1",
            "a7a8n"),
    ];

    [ParamsSource(nameof(UciMoveScenarios))]
    public UciMoveScenario Scenario { get; set; } = null!;

    private Position _position = null!;

    [IterationSetup]
    public void Setup()
    {
        _position = new Position(Scenario.Fen);
    }

    [Benchmark]
    public Move ParseUciMove()
    {
        return Helpers.MoveFromUci(_position, Scenario.UciMove);
    }

    public class UciMoveScenario(string label, string fen, string uciMove)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;
        public string UciMove { get; } = uciMove;

        public override string ToString() => Label;
    }
}
