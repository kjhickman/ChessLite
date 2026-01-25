using BenchmarkDotNet.Attributes;
using ChessLite;
using ChessLite.Movement;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 1_000_000)]
public class MoveGenerationBenchmarks
{
    public static IEnumerable<MoveGenerationScenario> MoveGenerationScenarios =>
    [
        new MoveGenerationScenario(
            "StartingPosition",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"),
        new MoveGenerationScenario(
            "ComplexMiddlegame",
            "r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"),
        new MoveGenerationScenario(
            "OpenPosition",
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"),
    ];

    [ParamsSource(nameof(MoveGenerationScenarios))]
    public MoveGenerationScenario MoveGenScenario { get; set; } = null!;

    private Game _game = null!;

    [IterationSetup]
    public void Setup()
    {
        _game = Game.ParseFen(MoveGenScenario.Fen);
    }

    [Benchmark]
    public int GetLegalMoves()
    {
        var count = 0;
        foreach (var move in _game.GetLegalMoves())
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int WriteLegalMoves()
    {
        Span<Move> moves = stackalloc Move[218];
        return _game.WriteLegalMoves(moves);
    }

    public class MoveGenerationScenario(string label, string fen)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;

        public override string ToString() => Label;
    }
}
