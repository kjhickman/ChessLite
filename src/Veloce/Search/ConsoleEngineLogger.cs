using Veloce.Core.Models;

namespace Veloce.Search;

/// <summary>
/// Basic implementation that outputs to console or specified TextWriter.
/// </summary>
public class ConsoleEngineLogger : IEngineLogger
{
    public void LogSearchInfo(SearchInfo info)
    {
        Console.WriteLine($"depth: {info.Depth}, score: {info.Score}, nodes: {info.NodesSearched}, time: {info.TimeElapsed.TotalMilliseconds:0}ms, nps: {info.NodesPerSecond}, hashfull: {info.HashFull}");
    }

    public void LogBestMove(Move? bestMove, Move? ponderMove = null)
    {
        Console.WriteLine($"bestmove: {bestMove}");
        if (ponderMove.HasValue && ponderMove.Value != Move.NullMove)
        {
            Console.WriteLine($"ponder: {ponderMove}");
        }
    }
}
