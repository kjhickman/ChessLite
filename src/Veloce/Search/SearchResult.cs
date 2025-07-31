using Veloce.Core.Models;

namespace Veloce.Search;

/// <summary>
/// Represents the result of a search operation
/// </summary>
public struct SearchResult
{
    /// <summary>
    /// The best move found by the search, or null if no legal moves exist
    /// </summary>
    public Move? BestMove { get; set; }

    /// <summary>
    /// The score of the position after the best move
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// The maximum depth that was fully searched
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// The number of nodes visited during the search
    /// </summary>
    public long NodesSearched { get; set; }

    /// <summary>
    /// The time taken to complete the search
    /// </summary>
    public TimeSpan TimeElapsed { get; set; }

    /// <summary>
    /// The state of the game after the best move
    /// </summary>
    public GameState GameState { get; set; }

    /// <summary>
    /// Returns the nodes per second (NPS) calculation
    /// </summary>
    public long NodesPerSecond =>
        TimeElapsed.TotalSeconds > 0
            ? (long)(NodesSearched / TimeElapsed.TotalSeconds)
            : 0;

    /// <summary>
    /// Returns if the score indicates a checkmate
    /// </summary>
    public bool IsMateScore => Math.Abs(Score) > 9000;

    /// <summary>
    /// Returns the mate in N moves (positive if we're winning, negative if we're losing)
    /// </summary>
    public int MateInMoves => IsMateScore ? Score > 0 ? (10000 - Score + 1) / 2 : (-10000 - Score + 1) / 2 : 0;
}
