using System.Numerics;
using Zugfish.Engine.Models;

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
        for (var piece = 0; piece < 12; piece++)
        {
            for (var square = 0; square < 64; square++)
            {
                PieceKeys[piece, square] = NextULong(rng);
            }
        }

        for (var i = 0; i < 16; i++)
            CastlingKeys[i] = NextULong(rng);

        for (var square = 0; square < 64; square++)
            EnPassantKeys[square] = NextULong(rng);

        SideKey = NextULong(rng);
    }

    private static ulong NextULong(Random rng)
    {
        var buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    public static ulong ComputeHash(Position pos)
    {
        ulong hash = 0;
        // For each piece type, iterate over its bitboard
        AddPieceHash(ref hash, pos.WhitePawns,   0);
        AddPieceHash(ref hash, pos.WhiteKnights,  1);
        AddPieceHash(ref hash, pos.WhiteBishops,  2);
        AddPieceHash(ref hash, pos.WhiteRooks,    3);
        AddPieceHash(ref hash, pos.WhiteQueens,   4);
        AddPieceHash(ref hash, pos.WhiteKing,     5);
        AddPieceHash(ref hash, pos.BlackPawns,    6);
        AddPieceHash(ref hash, pos.BlackKnights,  7);
        AddPieceHash(ref hash, pos.BlackBishops,  8);
        AddPieceHash(ref hash, pos.BlackRooks,    9);
        AddPieceHash(ref hash, pos.BlackQueens,   10);
        AddPieceHash(ref hash, pos.BlackKing,     11);

        hash ^= CastlingKeys[(byte)pos.CastlingRights];
        if (pos.EnPassantTarget != Square.None)
            hash ^= EnPassantKeys[(int)pos.EnPassantTarget];
        if (!pos.WhiteToMove)
            hash ^= SideKey;
        return hash;
    }

    private static void AddPieceHash(ref ulong hash, Bitboard board, int pieceIndex)
    {
        while (board != 0)
        {
            var square = BitOperations.TrailingZeroCount(board);
            hash ^= PieceKeys[pieceIndex, square];
            board &= board - 1;
        }
    }
}
