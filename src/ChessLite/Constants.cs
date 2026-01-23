namespace ChessLite;

internal static class Constants
{
    #region Bitboard / Masks

    // Ranks & Files
    internal const ulong SecondRank = 0xFF00;
    internal const ulong SeventhRank = 0xFF000000000000;
    internal const ulong FileA = 0x0101010101010101;
    internal const ulong FileH = 0x8080808080808080;

    // Castling Squares
    internal const ulong A1Mask = 1;
    internal const ulong B1Mask = 2;
    internal const ulong C1Mask = 4;
    internal const ulong D1Mask = 8;
    internal const ulong E1Mask = 16;
    internal const ulong F1Mask = 32;
    internal const ulong G1Mask = 64;
    internal const ulong H1Mask = 128;
    internal const ulong A8Mask = 0x100000000000000;
    internal const ulong B8Mask = 0x200000000000000;
    internal const ulong C8Mask = 0x400000000000000;
    internal const ulong D8Mask = 0x800000000000000;
    internal const ulong E8Mask = 0x1000000000000000;
    internal const ulong F8Mask = 0x2000000000000000;
    internal const ulong G8Mask = 0x4000000000000000;
    internal const ulong H8Mask = 0x8000000000000000;
    internal const ulong WhiteShortCastleEmptySquares = 0x60;
    internal const ulong WhiteLongCastleEmptySquares = 0xE;
    internal const ulong BlackShortCastleEmptySquares = 0x6000000000000000;
    internal const ulong BlackLongCastleEmptySquares = 0xE00000000000000;

    #endregion

    #region FENs

    internal const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    #endregion
}
