namespace ChessLite.Primitives;

/// <summary>
/// Represents special move types in chess that require additional handling.
/// </summary>
public enum SpecialMoveType
{
    /// <summary>A regular move with no special handling required.</summary>
    None = 0,

    /// <summary>A pawn advancing two squares from its starting position.</summary>
    DoublePawnPush = 1,

    /// <summary>An en passant capture.</summary>
    EnPassant = 2,

    /// <summary>Kingside castling (O-O).</summary>
    ShortCastle = 3,

    /// <summary>Queenside castling (O-O-O).</summary>
    LongCastle = 4,
}
