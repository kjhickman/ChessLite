namespace ChessLite.Primitives;

/// <summary>
/// Represents the piece type a pawn can be promoted to.
/// </summary>
[Flags]
public enum PromotedPieceType : byte
{
    /// <summary>No promotion.</summary>
    None = 0,

    /// <summary>Promote to knight.</summary>
    Knight = 1,

    /// <summary>Promote to bishop.</summary>
    Bishop = 2,

    /// <summary>Promote to rook.</summary>
    Rook = 4,

    /// <summary>Promote to queen.</summary>
    Queen = 8,
}