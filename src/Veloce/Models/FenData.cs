namespace Veloce.Models;

public class FenData
{
    public Bitboard WhitePawns { get; set; }
    public Bitboard WhiteKnights { get; set; }
    public Bitboard WhiteBishops { get; set; }
    public Bitboard WhiteRooks { get; set; }
    public Bitboard WhiteQueens { get; set; }
    public Bitboard WhiteKing { get; set; }
    public Bitboard BlackPawns { get; set; }
    public Bitboard BlackKnights { get; set; }
    public Bitboard BlackBishops { get; set; }
    public Bitboard BlackRooks { get; set; }
    public Bitboard BlackQueens { get; set; }
    public Bitboard BlackKing { get; set; }
    public bool WhiteToMove { get; set; }
    public CastlingRights CastlingRights { get; set; }
    public Square EnPassantTarget { get; set; }
    public int HalfmoveClock { get; set; }
}
