using ChessLite.Movement;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Parsing;

internal static class SanParser
{
    internal static Move MatchMove(Position position, ReadOnlySpan<char> san)
    {
        var trimmed = TrimAnnotations(san);
        if (trimmed.IsEmpty)
        {
            throw new ArgumentException("Invalid SAN token.", nameof(san));
        }

        if (TryParseCastle(trimmed, out var castleType))
        {
            return MatchCastleMove(position, castleType);
        }

        var promotion = PromotedPieceType.None;
        var promotionIndex = trimmed.IndexOf('=');
        if (promotionIndex >= 0)
        {
            if (promotionIndex + 1 >= trimmed.Length)
            {
                throw new ArgumentException("Invalid SAN promotion.", nameof(san));
            }

            promotion = ParsePromotion(trimmed[promotionIndex + 1]);
            trimmed = trimmed[..promotionIndex];
        }

        var isCapture = trimmed.IndexOf('x') >= 0;
        Span<char> cleanedBuffer = stackalloc char[trimmed.Length];
        var cleanedLength = 0;
        foreach (var ch in trimmed)
        {
            if (ch != 'x')
            {
                cleanedBuffer[cleanedLength++] = ch;
            }
        }

        var cleaned = cleanedBuffer[..cleanedLength];
        if (cleaned.Length < 2)
        {
            throw new ArgumentException("Invalid SAN token.", nameof(san));
        }

        var pieceType = PieceType.None;
        var startIndex = 0;
        if (IsPieceLetter(cleaned[0], out var pieceLetter))
        {
            pieceType = GetPieceType(pieceLetter, position.WhiteToMove);
            startIndex = 1;
        }
        else
        {
            pieceType = position.WhiteToMove ? PieceType.WhitePawn : PieceType.BlackPawn;
        }

        var destinationSpan = cleaned[^2..];
        var destination = ParseSquare(destinationSpan);
        var disambiguation = cleaned.Slice(startIndex, cleaned.Length - startIndex - 2);
        var fromFile = -1;
        var fromRank = -1;
        if (disambiguation.Length == 1)
        {
            var disambiguationChar = disambiguation[0];
            if (disambiguationChar is >= 'a' and <= 'h')
            {
                fromFile = disambiguationChar - 'a';
            }
            else if (disambiguationChar is >= '1' and <= '8')
            {
                fromRank = disambiguationChar - '1';
            }
            else
            {
                throw new ArgumentException("Invalid SAN disambiguation.", nameof(san));
            }
        }
        else if (disambiguation.Length == 2)
        {
            var fileChar = disambiguation[0];
            var rankChar = disambiguation[1];
            if (fileChar is not (>= 'a' and <= 'h') || rankChar is not (>= '1' and <= '8'))
            {
                throw new ArgumentException("Invalid SAN disambiguation.", nameof(san));
            }

            fromFile = fileChar - 'a';
            fromRank = rankChar - '1';
        }
        else if (disambiguation.Length > 2)
        {
            throw new ArgumentException("Invalid SAN disambiguation.", nameof(san));
        }

        if (promotion != PromotedPieceType.None && pieceType != (position.WhiteToMove ? PieceType.WhitePawn : PieceType.BlackPawn))
        {
            throw new ArgumentException("Invalid SAN promotion.", nameof(san));
        }

        if (pieceType == (position.WhiteToMove ? PieceType.WhitePawn : PieceType.BlackPawn) && !isCapture && disambiguation.Length > 0)
        {
            throw new ArgumentException("Invalid SAN pawn move.", nameof(san));
        }

        if (pieceType == (position.WhiteToMove ? PieceType.WhitePawn : PieceType.BlackPawn) && isCapture && disambiguation.Length == 0)
        {
            throw new ArgumentException("Invalid SAN pawn capture.", nameof(san));
        }

        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);
        var matches = 0;
        var matchedMove = Move.NullMove;
        for (var i = 0; i < moveCount; i++)
        {
            var move = moves[i];
            if (move.PieceType != pieceType)
            {
                continue;
            }

            if (move.To != destination)
            {
                continue;
            }

            if (move.IsCapture != isCapture)
            {
                continue;
            }

            if (promotion == PromotedPieceType.None)
            {
                if (move.PromotedPieceType != PromotedPieceType.None)
                {
                    continue;
                }
            }
            else if (move.PromotedPieceType != promotion)
            {
                continue;
            }

            if (fromFile >= 0 && ((int)move.From % 8) != fromFile)
            {
                continue;
            }

            if (fromRank >= 0 && ((int)move.From / 8) != fromRank)
            {
                continue;
            }

            matchedMove = move;
            matches++;
            if (matches > 1)
            {
                break;
            }
        }

        if (matches == 1)
        {
            return matchedMove;
        }

        throw new ArgumentException(matches == 0
            ? $"No legal move matches SAN '{san.ToString()}'."
            : $"SAN '{san.ToString()}' is ambiguous.",
            nameof(san));
    }

    internal static ReadOnlySpan<char> TrimAnnotations(ReadOnlySpan<char> token)
    {
        while (!token.IsEmpty)
        {
            var last = token[^1];
            if (last is '+' or '#' or '!' or '?')
            {
                token = token[..^1];
                continue;
            }

            break;
        }

        return token;
    }

    private static bool TryParseCastle(ReadOnlySpan<char> san, out SpecialMoveType castleType)
    {
        if (san.Equals("O-O", StringComparison.Ordinal) || san.Equals("0-0", StringComparison.Ordinal))
        {
            castleType = SpecialMoveType.ShortCastle;
            return true;
        }

        if (san.Equals("O-O-O", StringComparison.Ordinal) || san.Equals("0-0-0", StringComparison.Ordinal))
        {
            castleType = SpecialMoveType.LongCastle;
            return true;
        }

        castleType = SpecialMoveType.None;
        return false;
    }

    private static Move MatchCastleMove(Position position, SpecialMoveType castleType)
    {
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);
        for (var i = 0; i < moveCount; i++)
        {
            var move = moves[i];
            if (move.SpecialMoveType == castleType)
            {
                return move;
            }
        }

        throw new ArgumentException("No legal castling move available.", nameof(position));
    }

    private static bool IsPieceLetter(char token, out char piece)
    {
        if (token is 'N' or 'B' or 'R' or 'Q' or 'K')
        {
            piece = token;
            return true;
        }

        piece = '\0';
        return false;
    }

    private static PieceType GetPieceType(char piece, bool isWhite)
    {
        return piece switch
        {
            'N' => isWhite ? PieceType.WhiteKnight : PieceType.BlackKnight,
            'B' => isWhite ? PieceType.WhiteBishop : PieceType.BlackBishop,
            'R' => isWhite ? PieceType.WhiteRook : PieceType.BlackRook,
            'Q' => isWhite ? PieceType.WhiteQueen : PieceType.BlackQueen,
            'K' => isWhite ? PieceType.WhiteKing : PieceType.BlackKing,
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, "Invalid SAN piece."),
        };
    }

    private static PromotedPieceType ParsePromotion(char token)
    {
        return token switch
        {
            'Q' => PromotedPieceType.Queen,
            'R' => PromotedPieceType.Rook,
            'B' => PromotedPieceType.Bishop,
            'N' => PromotedPieceType.Knight,
            _ => throw new ArgumentException("Invalid SAN promotion.", nameof(token)),
        };
    }

    private static Square ParseSquare(ReadOnlySpan<char> squareToken)
    {
        if (squareToken.Length != 2)
        {
            throw new ArgumentException("Invalid SAN square.", nameof(squareToken));
        }

        var file = squareToken[0];
        var rank = squareToken[1];
        if (file is not (>= 'a' and <= 'h') || rank is not (>= '1' and <= '8'))
        {
            throw new ArgumentException("Invalid SAN square.", nameof(squareToken));
        }

        return SquareExtensions.FromRankFile(rank - '1', file - 'a');
    }
}
