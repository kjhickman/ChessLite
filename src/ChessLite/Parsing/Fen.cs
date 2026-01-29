using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Parsing;

/// <summary>
/// Provides methods to parse and write Forsyth-Edwards Notation (FEN).
/// </summary>
public static class Fen
{
    /// <summary>
    /// Creates a <see cref="Position"/> from a FEN (Forsyth-Edwards Notation) string.
    /// </summary>
    /// <param name="fen">The FEN string representing the chess position.</param>
    /// <returns>A new <see cref="Position"/> instance with the specified position.</returns>
    public static Position Parse(ReadOnlySpan<char> fen)
    {
        var position = new Position(initializeFromFen: false);
        if (!FenParser.Parse(fen, position))
        {
            throw new ArgumentException("Invalid FEN string", nameof(fen));
        }
        return position;
    }

    /// <summary>
    /// Attempts to parse a FEN (Forsyth-Edwards Notation) string into a <see cref="Position"/>.
    /// </summary>
    /// <param name="fen">The FEN string representing the chess position.</param>
    /// <param name="position">The parsed position if successful, otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<char> fen, out Position? position)
    {
        position = new Position(initializeFromFen: false);

        try
        {
            if (!FenParser.Parse(fen, position))
            {
                position = null;
                return false;
            }
            return true;
        }
        catch
        {
            position = null;
            return false;
        }
    }

    /// <summary>
    /// Formats the current position as a full FEN string.
    /// </summary>
    /// <param name="position">The position to serialize.</param>
    /// <returns>The FEN string describing the position.</returns>
    public static string Format(Position position)
    {
        return FenWriter.Format(position);
    }

    /// <summary>
    /// Formats the current game position as a full FEN string.
    /// </summary>
    /// <param name="game">The game containing the position to serialize.</param>
    /// <returns>The FEN string describing the position.</returns>
    public static string Format(Game game)
    {
        return FenWriter.Format(game);
    }
}
