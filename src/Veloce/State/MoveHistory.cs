using System.Runtime.InteropServices;
using Veloce.Core;

namespace Veloce.State;

[StructLayout(LayoutKind.Auto)]
public struct MoveHistory
{
    public Move Move;
    public CastlingRights PreviousCastlingRights;
    public Square PreviousEnPassantTarget;
    public int PreviousHalfmoveClock;
    public ulong PreviousZobristHash;
    public Bitboard PreviousWhiteAttacks;
    public Bitboard PreviousWhitePawnAttacks;
    public Bitboard PreviousWhiteKnightAttacks;
    public Bitboard PreviousWhiteKingAttacks;
    public Bitboard PreviousBlackAttacks;
    public Bitboard PreviousBlackPawnAttacks;
    public Bitboard PreviousBlackKnightAttacks;
    public Bitboard PreviousBlackKingAttacks;
    public Bitboard PreviousPinnedPieces;
}
