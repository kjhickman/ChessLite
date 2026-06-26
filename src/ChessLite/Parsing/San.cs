using ChessLite.Movement;
using ChessLite.State;

namespace ChessLite.Parsing;

/// <summary>
/// Provides methods to format Standard Algebraic Notation (SAN).
/// </summary>
public static class San
{
    /// <summary>
    /// Formats a single legal move in Standard Algebraic Notation (SAN).
    /// </summary>
    /// <param name="beforeMove">The position before the move is made.</param>
    /// <param name="move">The legal move to format.</param>
    /// <returns>The SAN text for <paramref name="move"/>.</returns>
    public static string Format(Position beforeMove, Move move)
    {
        return PgnWriter.FormatSan(beforeMove, move);
    }
}
