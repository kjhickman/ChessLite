namespace Zugfish.Engine;

public struct MoveUndo
{
    public Bitboard CapturedPiece; // Bitboard with only the captured piece
    public Bitboard FromSquare;    // Bitboard with only the moved piece at its original position
    public Bitboard ToSquare;      // Bitboard with only the moved piece at its new position
    public Move Move;              // The move that was made
    public ushort PreviousCastlingRights;
}
