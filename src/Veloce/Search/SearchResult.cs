using System.Runtime.InteropServices;
using Veloce.Core;

namespace Veloce.Search;

/// <summary>
/// Represents the result of a search operation
/// </summary>
[StructLayout(LayoutKind.Auto)]
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
}
