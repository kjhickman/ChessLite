namespace Zugfish.Engine;

public static class Zobrist
{
    // We map the 12 piece types in this order:
    // 0: White Pawn, 1: White Knight, 2: White Bishop,
    // 3: White Rook, 4: White Queen, 5: White King,
    // 6: Black Pawn, 7: Black Knight, 8: Black Bishop,
    // 9: Black Rook, 10: Black Queen, 11: Black King.
    public static readonly ulong[,] PieceKeys = new ulong[12, 64];
    public static readonly ulong[] CastlingKeys = new ulong[16];
    public static readonly ulong[] EnPassantKeys = new ulong[64];
    public static readonly ulong SideKey;

    static Zobrist()
    {
        var rng = new Random();
        for (int piece = 0; piece < 12; piece++)
        {
            for (int square = 0; square < 64; square++)
            {
                PieceKeys[piece, square] = NextULong(rng);
            }
        }

        for (int i = 0; i < 16; i++)
            CastlingKeys[i] = NextULong(rng);

        for (int square = 0; square < 64; square++)
            EnPassantKeys[square] = NextULong(rng);

        SideKey = NextULong(rng);
    }

    private static ulong NextULong(Random rng)
    {
        byte[] buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
}
