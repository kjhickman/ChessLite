using System.Text;
using ChessLite;
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
        var builder = new StringBuilder();
        AppendPiecePlacement(builder, position);
        builder.Append(position.WhiteToMove ? " w " : " b ");
        AppendCastlingRights(builder, position.CastlingRights);
        builder.Append(' ');
        AppendEnPassant(builder, position.EnPassantTarget);
        builder.Append(' ');
        builder.Append(position.HalfmoveClock);
        builder.Append(' ');
        builder.Append(position.FullmoveNumber);
        return builder.ToString();
    }

    /// <summary>
    /// Formats the current game position as a full FEN string.
    /// </summary>
    /// <param name="game">The game containing the position to serialize.</param>
    /// <returns>The FEN string describing the position.</returns>
    public static string Format(Game game)
    {
        return Format(game.Position);
    }

    private static void AppendPiecePlacement(StringBuilder builder, Position position)
    {
        for (var rank = 7; rank >= 0; rank--)
        {
            var emptyCount = 0;
            for (var file = 0; file < 8; file++)
            {
                var square = SquareExtensions.FromRankFile(rank, file);
                var piece = position.Mailbox[(int)square];
                if (piece == PieceType.None)
                {
                    emptyCount++;
                    continue;
                }

                if (emptyCount > 0)
                {
                    builder.Append(emptyCount);
                    emptyCount = 0;
                }

                builder.Append(PieceToChar(piece));
            }

            if (emptyCount > 0)
            {
                builder.Append(emptyCount);
            }

            if (rank > 0)
            {
                builder.Append('/');
            }
        }
    }

    private static void AppendCastlingRights(StringBuilder builder, CastlingRights castlingRights)
    {
        if (castlingRights == CastlingRights.None)
        {
            builder.Append('-');
            return;
        }

        if ((castlingRights & CastlingRights.WhiteKingside) != CastlingRights.None) builder.Append('K');
        if ((castlingRights & CastlingRights.WhiteQueenside) != CastlingRights.None) builder.Append('Q');
        if ((castlingRights & CastlingRights.BlackKingside) != CastlingRights.None) builder.Append('k');
        if ((castlingRights & CastlingRights.BlackQueenside) != CastlingRights.None) builder.Append('q');
    }

    private static void AppendEnPassant(StringBuilder builder, Square enPassantTarget)
    {
        if (enPassantTarget == Square.None)
        {
            builder.Append('-');
            return;
        }

        var file = (int)enPassantTarget % 8;
        var rank = (int)enPassantTarget / 8;
        builder.Append((char)('a' + file));
        builder.Append((char)('1' + rank));
    }

    private static char PieceToChar(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.WhitePawn => 'P',
            PieceType.WhiteKnight => 'N',
            PieceType.WhiteBishop => 'B',
            PieceType.WhiteRook => 'R',
            PieceType.WhiteQueen => 'Q',
            PieceType.WhiteKing => 'K',
            PieceType.BlackPawn => 'p',
            PieceType.BlackKnight => 'n',
            PieceType.BlackBishop => 'b',
            PieceType.BlackRook => 'r',
            PieceType.BlackQueen => 'q',
            PieceType.BlackKing => 'k',
            _ => throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, "Invalid piece type."),
        };
    }
}
