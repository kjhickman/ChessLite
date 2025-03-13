namespace Veloce;

public static class Constants
{
    #region Bitboard / Masks

    // Ranks & Files
    public const ulong SecondRank = 0xFF00;
    public const ulong SeventhRank = 0xFF000000000000;
    public const ulong FileA = 0x0101010101010101;
    public const ulong FileH = 0x8080808080808080;

    // Castling Squares

    public const ulong A1Mask = 1;
    public const ulong B1Mask = 2;
    public const ulong C1Mask = 4;
    public const ulong D1Mask = 8;
    public const ulong E1Mask = 16;
    public const ulong F1Mask = 32;
    public const ulong G1Mask = 64;
    public const ulong H1Mask = 128;
    public const ulong A8Mask = 0x100000000000000;
    public const ulong B8Mask = 0x200000000000000;
    public const ulong C8Mask = 0x400000000000000;
    public const ulong D8Mask = 0x800000000000000;
    public const ulong E8Mask = 0x1000000000000000;
    public const ulong F8Mask = 0x2000000000000000;
    public const ulong G8Mask = 0x4000000000000000;
    public const ulong H8Mask = 0x8000000000000000;
    public const ulong WhiteShortCastleEmptySquares = 0x60;
    public const ulong WhiteLongCastleEmptySquares = 0xE;
    public const ulong BlackShortCastleEmptySquares = 0x6000000000000000;
    public const ulong BlackLongCastleEmptySquares = 0xE00000000000000;

    #endregion

    #region FENs

    public const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    #endregion
}
