using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Parsing;

internal static class FenParser
{
    internal static bool Parse(ReadOnlySpan<char> fen, Position position)
    {
        // Split FEN into fields
        Span<Range> ranges = stackalloc Range[6];
        var fieldCount = fen.Split(ranges, ' ');
        if (fieldCount < 4) return false; // Need at least 4 fields

        // Parse piece placement (field 0)
        var piecePlacement = fen[ranges[0]];
        var square = Square.a8;

        for (var i = 0; i < piecePlacement.Length; i++)
        {
            var c = piecePlacement[i];
            if (char.IsDigit(c))
            {
                square += c - '0'; // Empty squares
            }
            else if (c == '/')
            {
                square -= 16; // Move to next rank
            }
            else
            {
                if ((int)square < 0 || (int)square > 63) return false;

                var pieceMask = Bitboard.Mask(square++);
                switch (c)
                {
                    case 'P': position.WhitePawns |= pieceMask; break;
                    case 'N': position.WhiteKnights |= pieceMask; break;
                    case 'B': position.WhiteBishops |= pieceMask; break;
                    case 'R': position.WhiteRooks |= pieceMask; break;
                    case 'Q': position.WhiteQueens |= pieceMask; break;
                    case 'K': position.WhiteKing |= pieceMask; break;
                    case 'p': position.BlackPawns |= pieceMask; break;
                    case 'n': position.BlackKnights |= pieceMask; break;
                    case 'b': position.BlackBishops |= pieceMask; break;
                    case 'r': position.BlackRooks |= pieceMask; break;
                    case 'q': position.BlackQueens |= pieceMask; break;
                    case 'k': position.BlackKing |= pieceMask; break;
                    default: return false; // Invalid piece character
                }
            }
        }

        // Parse active color (field 1)
        var activeColor = fen[ranges[1]];
        if (activeColor.Length != 1) return false;
        position.WhiteToMove = activeColor[0] switch
        {
            'w' => true,
            'b' => false,
            _ => false
        };
        if (activeColor[0] != 'w' && activeColor[0] != 'b') return false;

        // Parse castling rights (field 2)
        var castlingRights = fen[ranges[2]];
        position.CastlingRights = CastlingRights.None;
        for (var i = 0; i < castlingRights.Length; i++)
        {
            switch (castlingRights[i])
            {
                case 'K': position.CastlingRights |= CastlingRights.WhiteKingside; break;
                case 'Q': position.CastlingRights |= CastlingRights.WhiteQueenside; break;
                case 'k': position.CastlingRights |= CastlingRights.BlackKingside; break;
                case 'q': position.CastlingRights |= CastlingRights.BlackQueenside; break;
                case '-': break; // No castling rights
                default: return false; // Invalid character
            }
        }

        // Parse en passant square (field 3)
        var enPassantSquare = fen[ranges[3]];
        if (enPassantSquare.Length == 1 && enPassantSquare[0] == '-')
        {
            position.EnPassantTarget = Square.None;
        }
        else if (enPassantSquare.Length == 2)
        {
            var file = enPassantSquare[0] - 'a';
            var rank = enPassantSquare[1] - '1';
            if (file < 0 || file > 7 || rank < 0 || rank > 7) return false;
            position.EnPassantTarget = (Square)(rank * 8 + file);
        }
        else
        {
            return false;
        }

        // Parse halfmove clock (field 4) - optional
        if (fieldCount >= 5)
        {
            var halfmoveClock = fen[ranges[4]];
            if (!int.TryParse(halfmoveClock, out var halfmove)) return false;
            position.HalfmoveClock = halfmove;
        }
        else
        {
            position.HalfmoveClock = 0;
        }

        // Parse fullmove number (field 5) - optional
        if (fieldCount >= 6)
        {
            var fullmoveNumber = fen[ranges[5]];
            if (!int.TryParse(fullmoveNumber, out var fullmove)) return false;
            if (fullmove < 1) return false;
            position.FullmoveNumber = fullmove;
        }
        else
        {
            position.FullmoveNumber = 1;
        }

        // Update derived state
        position.UpdateCombinedBitboards();
        position.UpdateMailbox();
        position.UpdateAttacks();
        position.UpdatePinnedPieces();
        position.ZobristHash = Zobrist.ComputeHash(position);

        return true;
    }
}
