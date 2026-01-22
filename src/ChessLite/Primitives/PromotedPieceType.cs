namespace ChessLite.Primitives;

[Flags]
public enum PromotedPieceType : byte
{
    None = 0,
    Knight = 1,
    Bishop = 2,
    Rook = 4,
    Queen = 8,
}