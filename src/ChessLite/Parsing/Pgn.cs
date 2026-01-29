using ChessLite;

namespace ChessLite.Parsing;

/// <summary>
/// Provides methods to parse Portable Game Notation (PGN).
/// </summary>
public static class Pgn
{
    /// <summary>
    /// Creates a <see cref="Game"/> from a PGN (Portable Game Notation) string.
    /// </summary>
    /// <param name="pgn">The PGN string representing the game.</param>
    /// <returns>A new <see cref="Game"/> instance with the specified move history.</returns>
    public static Game Parse(ReadOnlySpan<char> pgn)
    {
        return PgnParser.Parse(pgn);
    }

    /// <summary>
    /// Attempts to parse a PGN (Portable Game Notation) string into a <see cref="Game"/>.
    /// </summary>
    /// <param name="pgn">The PGN string representing the game.</param>
    /// <param name="game">The parsed game if successful, otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<char> pgn, out Game? game)
    {
        try
        {
            game = PgnParser.Parse(pgn);
            return true;
        }
        catch
        {
            game = null;
            return false;
        }
    }
}
