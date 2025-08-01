using Veloce.Core.Models;

namespace Veloce.State;

public static class FenParser
{
    public static FenData ParseFen(ReadOnlySpan<char> fen)
    {
        var result = new FenData();

        Span<Range> ranges = stackalloc Range[6]; // FEN has 6 fields
        fen.Split(ranges, ' ');

        var piecePlacement = fen[ranges[0]];
        var square = Square.a8;
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

        // Active color (index 1)
        var activeColor = fen[ranges[1]];
        result.WhiteToMove = activeColor[0] switch
        {
            'w' => true,
            'b' => false,
            _ => throw new ArgumentException("Invalid active color."),
        };

        // Parse castling rights (index 2)
        var castlingRights = fen[ranges[2]];

        if (castlingRights.IndexOf('K') != -1) result.CastlingRights |= CastlingRights.WhiteKingside;
        if (castlingRights.IndexOf('Q') != -1) result.CastlingRights |= CastlingRights.WhiteQueenside;
        if (castlingRights.IndexOf('k') != -1) result.CastlingRights |= CastlingRights.BlackKingside;
        if (castlingRights.IndexOf('q') != -1) result.CastlingRights |= CastlingRights.BlackQueenside;

        // Parse en passant square (index 3)
        var enPassantSquare = fen[ranges[3]];
        if (enPassantSquare is not "-")
        {
            result.EnPassantTarget = Enum.Parse<Square>(enPassantSquare);
        }
        else
        {
            result.EnPassantTarget = Square.None;
        }

        // Halfmove clock (index 4)
        var halfmoveClock = fen[ranges[4]];
        result.HalfmoveClock = int.Parse(halfmoveClock);

        // Index 5 is the fullmove number, which we do no use

        return result;
    }
}
