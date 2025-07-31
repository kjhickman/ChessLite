using Veloce.Core.Models;

namespace Veloce.Search;

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
