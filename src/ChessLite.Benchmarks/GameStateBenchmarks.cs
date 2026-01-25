using BenchmarkDotNet.Attributes;
using ChessLite;
using ChessLite.Movement;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 1_000_000)]
public class GameStateBenchmarks
{
    public static IEnumerable<GameStateScenario> GetStateScenarios =>
    [
        new GameStateScenario(
            "StartingPosition",
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            0),
        new GameStateScenario(
            "Midgame",
            "r1bqkb1r/pppp1ppp/2n2n2/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4",
            20),
        new GameStateScenario(
            "NearRepetition",
            "8/8/1k6/6N1/1pp3P1/1P3K1p/8/8 w - - 48 25",
            100),
        new GameStateScenario(
            "Checkmate",
            "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3",
            0),
        new GameStateScenario(
            "Stalemate",
            "k7/8/1Q6/8/8/8/8/7K b - - 0 1",
            0),
        new GameStateScenario(
            "InsufficientMaterial",
            "k7/8/1K6/8/8/8/8/7N w - - 0 1",
            0),
    ];

    [ParamsSource(nameof(GetStateScenarios))]
    public GameStateScenario StateScenario { get; set; } = null!;

    private Game _game = null!;

    [IterationSetup]
    public void Setup()
    {
        _game = Game.ParseFen(StateScenario.Fen);

        // Simulate moves to build up ply count
        Span<Move> moves = stackalloc Move[218];
        for (var i = 0; i < StateScenario.PlyCount; i++)
        {
            var moveCount = _game.WriteLegalMoves(moves);
            if (moveCount > 0)
            {
                _game.MakeMove(moves[0]);
            }
        }
    }

    [Benchmark]
    public GameState GetState()
    {
        return _game.GetState();
    }

    public class GameStateScenario(string label, string fen, int plyCount)
    {
        public string Label { get; } = label;
        public string Fen { get; } = fen;
        public int PlyCount { get; } = plyCount;

        public override string ToString() => Label;
    }
}
