using System.Runtime.InteropServices;
using ChessLite.Primitives;

namespace ChessLite.Movement;

[StructLayout(LayoutKind.Auto)]
internal struct MoveHistory
{
    internal Move Move;
    internal CastlingRights PreviousCastlingRights;
    internal Square PreviousEnPassantTarget;
    internal int PreviousHalfmoveClock;
    internal ulong PreviousZobristHash;
    internal Bitboard PreviousWhiteAttacks;
    internal Bitboard PreviousWhitePawnAttacks;
    internal Bitboard PreviousWhiteKnightAttacks;
    internal Bitboard PreviousWhiteKingAttacks;
    internal Bitboard PreviousBlackAttacks;
    internal Bitboard PreviousBlackPawnAttacks;
    internal Bitboard PreviousBlackKnightAttacks;
    internal Bitboard PreviousBlackKingAttacks;
    internal Bitboard PreviousPinnedPieces;
}
