using ChessLite.Primitives;

namespace ChessLite.Parsing;

internal class FenData
{
    internal Bitboard WhitePawns { get; set; }
    internal Bitboard WhiteKnights { get; set; }
    internal Bitboard WhiteBishops { get; set; }
    internal Bitboard WhiteRooks { get; set; }
    internal Bitboard WhiteQueens { get; set; }
    internal Bitboard WhiteKing { get; set; }
    internal Bitboard BlackPawns { get; set; }
    internal Bitboard BlackKnights { get; set; }
    internal Bitboard BlackBishops { get; set; }
    internal Bitboard BlackRooks { get; set; }
    internal Bitboard BlackQueens { get; set; }
    internal Bitboard BlackKing { get; set; }
    internal bool WhiteToMove { get; set; }
    internal CastlingRights CastlingRights { get; set; }
    internal Square EnPassantTarget { get; set; }
    internal int HalfmoveClock { get; set; }
}
