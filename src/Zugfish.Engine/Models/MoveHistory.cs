namespace Zugfish.Engine.Models;

public struct MoveHistory
{
    public Move Move;
    public CastlingRights PreviousCastlingRights;
    public Square PreviousEnPassantTarget;
    public int PreviousHalfmoveClock;
    public ulong PreviousZobristHash;
    public Bitboard PreviousWhiteAttacks;
    public Bitboard PreviousBlackAttacks;
}
