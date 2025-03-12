using Zugfish.Engine.Models;

namespace Zugfish.Engine;

/// <summary>
/// Interface for logging search information, allowing different implementations (console, UCI, etc.)
/// </summary>
public interface IEngineLogger
{
    /// <summary>
    /// Logs information about the current search iteration.
    /// </summary>
    void LogSearchInfo(SearchInfo info);

    /// <summary>
    /// Logs the best move found at the end of search.
    /// </summary>
    void LogBestMove(Move? bestMove, Move? ponderMove = null);
}

/// <summary>
/// Container for search information that can be passed to loggers.
/// </summary>
public class SearchInfo
{
    public int Depth { get; set; }
    public int Score { get; set; }
    public bool IsMateScore { get; set; }
    public long NodesSearched { get; set; }
    public TimeSpan TimeElapsed { get; set; }
    public long NodesPerSecond { get; set; }
}

/// <summary>
/// Basic implementation that outputs to console or specified TextWriter.
/// </summary>
public class ConsoleEngineLogger : IEngineLogger
{
    public void LogSearchInfo(SearchInfo info)
    {
        Console.WriteLine($"depth: {info.Depth}, score: {info.Score}, nodes: {info.NodesSearched}, time: {info.TimeElapsed.TotalMilliseconds:0}ms, nps: {info.NodesPerSecond}");
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

public class NullEngineLogger : IEngineLogger
{
    public void LogSearchInfo(SearchInfo info)
    {
    }

    public void LogBestMove(Move? bestMove, Move? ponderMove = null)
    {
    }
}
