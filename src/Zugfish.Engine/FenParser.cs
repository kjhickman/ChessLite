using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public static class FenParser
{
    public static FenData ParseFen(ReadOnlySpan<char> fen)
    {
        var result = new FenData();
        var enumerator = fen.Split(' ');
        enumerator.MoveNext();

        // Parse piece placement
        var piecePlacement = fen[enumerator.Current]; enumerator.MoveNext();
        var square = 56; // Start at a8
        for (var i = 0; i < piecePlacement.Length; i++)
        {
            var c = piecePlacement[i];
            if (char.IsDigit(c))
                square += c - '0'; // Empty squares
            else if (c == '/')
                square -= 16; // Move to next rank
            else
            {
                var pieceMask = Bitboard.Mask(square++);
                switch (c)
                {
                    case 'P':
                        result.WhitePawns = result.WhitePawns.SetSquares(pieceMask);
                        break;
                    case 'N':
                        result.WhiteKnights = result.WhiteKnights.SetSquares(pieceMask);
                        break;
                    case 'B':
                        result.WhiteBishops = result.WhiteBishops.SetSquares(pieceMask);
                        break;
                    case 'R':
                        result.WhiteRooks = result.WhiteRooks.SetSquares(pieceMask);
                        break;
                    case 'Q':
                        result.WhiteQueens = result.WhiteQueens.SetSquares(pieceMask);
                        break;
                    case 'K':
                        result.WhiteKing = result.WhiteKing.SetSquares(pieceMask);
                        break;
                    case 'p':
                        result.BlackPawns = result.BlackPawns.SetSquares(pieceMask);
                        break;
                    case 'n':
                        result.BlackKnights = result.BlackKnights.SetSquares(pieceMask);
                        break;
                    case 'b':
                        result.BlackBishops = result.BlackBishops.SetSquares(pieceMask);
                        break;
                    case 'r':
                        result.BlackRooks = result.BlackRooks.SetSquares(pieceMask);
                        break;
                    case 'q':
                        result.BlackQueens = result.BlackQueens.SetSquares(pieceMask);
                        break;
                    case 'k':
                        result.BlackKing = result.BlackKing.SetSquares(pieceMask);
                        break;
                    default:
                        throw new ArgumentException($"Invalid FEN piece: {c}");
                }
            }
        }

        var activeColor = fen[enumerator.Current]; enumerator.MoveNext();
        result.WhiteToMove = activeColor[0] switch
        {
            'w' => true,
            'b' => false,
            _ => throw new ArgumentException("Invalid active color.")
        };

        // Parse castling rights
        var castlingRights = fen[enumerator.Current]; enumerator.MoveNext();

        if (castlingRights.IndexOf('K') != -1) result.CastlingRights |= CastlingRights.WhiteKingside;
        if (castlingRights.IndexOf('Q') != -1) result.CastlingRights |= CastlingRights.WhiteQueenside;
        if (castlingRights.IndexOf('k') != -1) result.CastlingRights |= CastlingRights.BlackKingside;
        if (castlingRights.IndexOf('q') != -1) result.CastlingRights |= CastlingRights.BlackQueenside;

        // Parse en passant square
        var enPassantSquare = fen[enumerator.Current]; enumerator.MoveNext();
        if (enPassantSquare is not "-")
        {
            result.EnPassantTarget = Enum.Parse<Square>(enPassantSquare);
        }
        else
        {
            result.EnPassantTarget = Square.None;
        }

        var halfmoveClock = fen[enumerator.Current]; enumerator.MoveNext();
        result.HalfmoveClock = int.Parse(halfmoveClock);

        return result;
    }
}
