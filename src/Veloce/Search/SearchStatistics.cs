using System.Runtime.InteropServices;

namespace Veloce.Search;

/// <summary>
/// Statistics collected during search
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SearchStatistics
{
    /// <summary>
    /// Total nodes searched
    /// </summary>
    public long NodesSearched { get; set; }

    /// <summary>
    /// Transposition table hits
    /// </summary>
    public long TranspositionHits { get; set; }

    /// <summary>
    /// Alpha-beta cutoffs
    /// </summary>
    public long AlphaBetaCutoffs { get; set; }

    /// <summary>
    /// Quiescence search nodes
    /// </summary>
    public long QuiescenceNodes { get; set; }
}
