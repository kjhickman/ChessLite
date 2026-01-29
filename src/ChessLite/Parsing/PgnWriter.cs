using System.Text;
using ChessLite.Movement;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Parsing;

internal static class PgnWriter
{
    internal static string Format(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var builder = new StringBuilder();
        var position = new Position();
        var executor = new MoveExecutor();

        var moveIndex = 0;
        foreach (var move in game.GetMoveHistory())
        {
            if (moveIndex > 0)
            {
                builder.Append(' ');
            }

            if (position.WhiteToMove)
            {
                builder.Append(position.FullmoveNumber);
                builder.Append(". ");
            }

            builder.Append(WriteSanMove(position, move, executor));
            executor.MakeMove(position, move);
            moveIndex++;
        }

        if (moveIndex > 0)
        {
            builder.Append(' ');
        }

        builder.Append(WriteResult(game));
        return builder.ToString();
    }

    private static string WriteResult(Game game)
    {
        var state = game.GetState();
        if (state == GameState.Ongoing)
        {
            return "*";
        }

        if (state.IsDraw())
        {
            return "1/2-1/2";
        }

        return game.Position.WhiteToMove ? "0-1" : "1-0";
    }

    private static string WriteSanMove(Position position, Move move, MoveExecutor executor)
    {
        if (move.SpecialMoveType == SpecialMoveType.ShortCastle)
        {
            return AppendCheckSuffix(position, move, "O-O", executor);
        }

        if (move.SpecialMoveType == SpecialMoveType.LongCastle)
        {
            return AppendCheckSuffix(position, move, "O-O-O", executor);
        }

        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);

        var pieceChar = GetPieceLetter(move.PieceType);
        var requiresFile = false;
        var requiresRank = false;

        if (pieceChar != '\0')
        {
            var ambiguousCount = 0;
            var fromFile = GetFile(move.From);
            var fromRank = GetRank(move.From);
            for (var i = 0; i < moveCount; i++)
            {
                var candidate = moves[i];
                if (candidate == move)
                {
                    continue;
                }

                if (candidate.PieceType != move.PieceType || candidate.To != move.To)
                {
                    continue;
                }

                if (candidate.PromotedPieceType != move.PromotedPieceType)
                {
                    continue;
                }

                if (candidate.IsCapture != move.IsCapture)
                {
                    continue;
                }

                ambiguousCount++;
                if (GetFile(candidate.From) != fromFile)
                {
                    requiresFile = true;
                }
                else
                {
                    requiresRank = true;
                }
            }

            if (ambiguousCount > 0 && !requiresFile && !requiresRank)
            {
                requiresFile = true;
            }
        }

        var builder = new StringBuilder();
        if (pieceChar != '\0')
        {
            builder.Append(pieceChar);
            if (requiresFile)
            {
                builder.Append((char)('a' + GetFile(move.From)));
            }
            if (requiresRank)
            {
                builder.Append((char)('1' + GetRank(move.From)));
            }
        }
        else if (move.IsCapture)
        {
            builder.Append((char)('a' + GetFile(move.From)));
        }

        if (move.IsCapture)
        {
            builder.Append('x');
        }

        AppendSquare(builder, move.To);

        if (move.PromotedPieceType != PromotedPieceType.None)
        {
            builder.Append('=');
            builder.Append(GetPromotionLetter(move.PromotedPieceType));
        }

        return AppendCheckSuffix(position, move, builder.ToString(), executor);
    }

    private static string AppendCheckSuffix(Position position, Move move, string san, MoveExecutor executor)
    {
        var next = position.Clone();
        executor.MakeMove(next, move);
        var isCheck = MoveGeneration.IsSquareAttacked(next,
            next.WhiteToMove ? next.WhiteKing.GetFirstSquare() : next.BlackKing.GetFirstSquare(),
            byWhite: !next.WhiteToMove);
        if (!isCheck)
        {
            return san;
        }

        Span<Move> moves = stackalloc Move[218];
        var replyCount = MoveGeneration.GenerateLegalMoves(next, moves);
        return replyCount == 0 ? san + '#' : san + '+';
    }

    private static void AppendSquare(StringBuilder builder, Square square)
    {
        builder.Append((char)('a' + GetFile(square)));
        builder.Append((char)('1' + GetRank(square)));
    }

    private static char GetPieceLetter(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.WhiteKnight => 'N',
            PieceType.WhiteBishop => 'B',
            PieceType.WhiteRook => 'R',
            PieceType.WhiteQueen => 'Q',
            PieceType.WhiteKing => 'K',
            PieceType.BlackKnight => 'N',
            PieceType.BlackBishop => 'B',
            PieceType.BlackRook => 'R',
            PieceType.BlackQueen => 'Q',
            PieceType.BlackKing => 'K',
            _ => '\0'
        };
    }

    private static char GetPromotionLetter(PromotedPieceType promotedPieceType)
    {
        return promotedPieceType switch
        {
            PromotedPieceType.Queen => 'Q',
            PromotedPieceType.Rook => 'R',
            PromotedPieceType.Bishop => 'B',
            PromotedPieceType.Knight => 'N',
            _ => throw new ArgumentOutOfRangeException(nameof(promotedPieceType), promotedPieceType, "Invalid promotion type.")
        };
    }

    private static int GetFile(Square square)
    {
        return (int)square % 8;
    }

    private static int GetRank(Square square)
    {
        return (int)square / 8;
    }
}
