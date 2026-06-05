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
    internal int PreviousFullmoveNumber;
    internal ulong PreviousZobristHash;
    internal Bitboard PreviousWhiteAttacks;
    internal Bitboard PreviousWhiteAttacksWithoutBlackKing;
    internal Bitboard PreviousBlackAttacks;
    internal Bitboard PreviousBlackAttacksWithoutWhiteKing;
}
