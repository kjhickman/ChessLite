using BenchmarkDotNet.Attributes;
using ChessLite;
using ChessLite.Movement;

namespace ChessLite.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 500_000)]
public class MoveHistoryBenchmarks
{
    public static IEnumerable<MoveHistoryScenario> MoveHistoryScenarios =>
    [
        new MoveHistoryScenario("TenMoves", 10),
        new MoveHistoryScenario("FiftyMoves", 50),
        new MoveHistoryScenario("HundredMoves", 100),
    ];

    [ParamsSource(nameof(MoveHistoryScenarios))]
    public MoveHistoryScenario HistoryScenario { get; set; } = null!;

    private Game _game = null!;

    [IterationSetup]
    public void Setup()
    {
        _game = new Game();
        Span<Move> moves = stackalloc Move[218];

        // Play moves to build up history
        for (var i = 0; i < HistoryScenario.MoveCount; i++)
        {
            var moveCount = _game.WriteLegalMoves(moves);
            if (moveCount > 0)
            {
                _game.MakeMove(moves[0]);
            }
            else
            {
                break;
            }
        }
    }

    [Benchmark]
    public Move[] GetMoveHistory()
    {
        return _game.GetMoveHistory().ToArray();
    }

    public class MoveHistoryScenario(string label, int moveCount)
    {
        public string Label { get; } = label;
        public int MoveCount { get; } = moveCount;

        public override string ToString() => Label;
    }
}
