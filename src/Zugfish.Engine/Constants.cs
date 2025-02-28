namespace Zugfish.Engine;

public static class Constants
{
    // Bitboard Masks

    // Ranks & Files
    public const ulong SecondRank = 0xFF00;
    public const ulong SeventhRank = 0xFF000000000000;
    public const ulong FileA = 0x0101010101010101;
    public const ulong FileH = 0x8080808080808080;

    // Castling Squares
    public const ulong WhiteShortCastleEmptySquares = 0x60;
    public const ulong WhiteLongCastleEmptySquares = 0xE;
    public const ulong BlackShortCastleEmptySquares = 0x6000000000000000;
    public const ulong BlackLongCastleEmptySquares = 0xE000000000000000;

}
