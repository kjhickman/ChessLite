using System.Text;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Parsing;

internal static class FenWriter
{
    internal static string Format(Position position)
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

    internal static string Format(Game game)
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
                var piece = position.PieceAt(square);
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
