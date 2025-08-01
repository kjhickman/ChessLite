using System.Runtime.InteropServices;
using Veloce.Core;

namespace Veloce.Search;

/// <summary>
/// Unified context for search operations containing all necessary state
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SearchContext
{
    /// <summary>
    /// Current search depth
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Current ply (distance from root)
    /// </summary>
    public int Ply { get; set; }

    /// <summary>
    /// Alpha value for alpha-beta pruning
    /// </summary>
    public int Alpha { get; set; }

    /// <summary>
    /// Beta value for alpha-beta pruning
    /// </summary>
    public int Beta { get; set; }

    /// <summary>
    /// Principal variation (best line of play)
    /// </summary>
    public Move[] PrincipalVariation { get; set; }

    /// <summary>
    /// Search statistics and metrics
    /// </summary>
    public SearchStatistics Stats { get; set; }

    /// <summary>
    /// Creates a new search context with default values
    /// </summary>
    public static SearchContext CreateRoot(int depth, int alpha, int beta)
    {
        return new SearchContext
        {
            Depth = depth,
            Ply = 1,
            Alpha = alpha,
            Beta = beta,
            PrincipalVariation = new Move[depth],
            Stats = new SearchStatistics(),
        };
    }

    /// <summary>
    /// Creates a child context for the next ply
    /// </summary>
    public readonly SearchContext CreateChild(int newDepth, int newAlpha, int newBeta)
    {
        return new SearchContext
        {
            Depth = newDepth,
            Ply = Ply + 1,
            Alpha = newAlpha,
            Beta = newBeta,
            PrincipalVariation = PrincipalVariation,
            Stats = Stats,
        };
    }
}
