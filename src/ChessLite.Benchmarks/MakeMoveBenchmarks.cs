using BenchmarkDotNet.Attributes;
using ChessLite.Movement;
using ChessLite.Parsing;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 200_000)]
public class MakeMoveBenchmarks
{
    private const int MoveBufferSize = 218;

    public static IEnumerable<MakeMoveScenario> MakeMoveScenarios =>
    [
        new MakeMoveScenario(
            "StartingPosition",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"),
        new MakeMoveScenario(
            "ComplexMiddlegame",
            "r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"),
        new MakeMoveScenario(
            "OpenPosition",
            "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"),
        new MakeMoveScenario(
            "PromotionPosition",
            "8/P6k/8/8/8/8/6Kp/8 w - - 0 1"),
        new MakeMoveScenario(
            "EnPassantPosition",
            "8/8/8/3pP3/8/8/8/4K2k w - d6 0 1"),
    ];

    [ParamsSource(nameof(MakeMoveScenarios))]
    public MakeMoveScenario Scenario { get; set; } = null!;

    private readonly Move[] _moves = new Move[MoveBufferSize];
    private Game _game = null!;
    private int _moveCount;

    [IterationSetup]
    public void Setup()
    {
        _game = new Game(Fen.Parse(Scenario.Fen));

        Span<Move> moves = stackalloc Move[MoveBufferSize];
        _moveCount = _game.WriteLegalMoves(moves);
        moves[.._moveCount].CopyTo(_moves);
    }

    [Benchmark]
    public int MakeMoveAndUndo()
    {
        for (var i = 0; i < _moveCount; i++)
        {
            _game.MakeMove(_moves[i]);
            _game.UndoMove();
        }

        return _moveCount;
    }

    public class MakeMoveScenario(string label, string fen)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;

        public override string ToString() => Label;
    }
}
