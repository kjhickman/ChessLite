using BenchmarkDotNet.Attributes;
using ChessLite;
using ChessLite.Movement;
using ChessLite.Parsing;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
public class PerftBenchmarks
{
    public static IEnumerable<PositionParam> PositionSource =>
    [
        new PositionParam("StartingPosition", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 5),
        new PositionParam("GeneralEndgame", "8/8/1k6/6N1/1pp3P1/1P3K1p/8/8 w - - 0 9", 6),
        new PositionParam("BishopAndPawnEndgame", "8/8/8/5KB1/p7/P5k1/8/8 w - - 0 1", 7),
        new PositionParam("RookAndPawnEndgame", "3K4/3P1k2/8/8/8/8/4R3/2r5 w - - 0 1", 6),
    ];

    [ParamsSource(nameof(PositionSource))]
    public PositionParam Position { get; set; } = null!;

    private Game _game = null!;

    [IterationSetup]
    public void Setup()
    {
        _game = Game.FromFen(Position.Fen);
    }

    [Benchmark]
    public long Perft()
    {
        return CountNodes(_game, Position.Depth);
    }

    private static long CountNodes(Game game, int depth)
    {
        if (depth == 0) return 1;

        Span<Move> moves = stackalloc Move[218];
        var moveCount = game.WriteLegalMoves(moves);

        if (depth == 1) return moveCount;

        long nodes = 0;
        for (var i = 0; i < moveCount; i++)
        {
            game.MakeMove(moves[i]);
            nodes += CountNodes(game, depth - 1);
            game.UndoMove();
        }

        return nodes;
    }

    public class PositionParam(string label, string fen, int depth)
    {
        public string Label { get; set; } = label;
        public string Fen { get; set; } = fen;
        public int Depth { get; set; } = depth;

        public override string ToString() => Label;
    }
}


