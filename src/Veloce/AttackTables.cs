using Veloce.Extensions;
using Veloce.Models;

namespace Veloce;

public static class AttackTables
{
    public static readonly Bitboard[] KnightAttacks = new Bitboard[64];
    public static readonly Bitboard[] KingAttacks = new Bitboard[64];
    public static readonly Bitboard[] WhitePawnAttacks = new Bitboard[64];
    public static readonly Bitboard[] BlackPawnAttacks = new Bitboard[64];

    static AttackTables()
    {
        InitializeKnightAttacks();
        InitializeKingAttacks();
        InitializePawnAttacks();
    }

    private static void InitializeKnightAttacks()
    {
        // Knight offsets
        Span<int> knightOffsets = [17, 15, 10, 6, -6, -10, -15, -17];

        for (var i = 0; i < 64; i++)
        {
            var square = (Square)i;
            Bitboard attacks = 0;
            var fromFile = square.GetFile();
            var fromRank = square.GetRank();

            for (var j = 0; j < knightOffsets.Length; j++)
            {
                var toSquare = square + knightOffsets[j];

                if (!toSquare.IsValid())
                    continue; // Out of board

                var toFile = toSquare.GetFile();
                var toRank = toSquare.GetRank();

                // Check for wraparound
                if (Math.Abs(toFile - fromFile) > 2 || Math.Abs(toRank - fromRank) > 2)
                    continue;

                attacks |= Bitboard.Mask(toSquare);
            }

            KnightAttacks[i] = attacks;
        }
    }

    private static void InitializeKingAttacks()
    {
        for (var i = 0; i < 64; i++)
        {
            var square = (Square)i;
            Bitboard attacks = 0;
            var fromFile = square.GetFile();
            var fromRank = square.GetRank();

            for (var fileOffset = -1; fileOffset <= 1; fileOffset++)
            {
                for (var rankOffset = -1; rankOffset <= 1; rankOffset++)
                {
                    if (fileOffset == 0 && rankOffset == 0)
                        continue; // Skip the king's position

                    var toFile = fromFile + fileOffset;
                    var toRank = fromRank + rankOffset;

                    if (toFile < 0 || toFile > 7 || toRank < 0 || toRank > 7)
                        continue; // Out of board

                    var toSquare = (Square)(toRank * 8 + toFile);
                    attacks |= Bitboard.Mask(toSquare);
                }
            }

            KingAttacks[i] = attacks;
        }
    }

    private static void InitializePawnAttacks()
    {
        for (var i = 0; i < 64; i++)
        {
            var square = (Square)i;
            var fromFile = square.GetFile();
            var fromRank = square.GetRank();

            Bitboard whiteAttacks = 0;
            if (fromRank < 7) // Not on 8th rank
            {
                if (fromFile > 0) // Not on a-file
                    whiteAttacks |= Bitboard.Mask(square + 7); // Up-left

                if (fromFile < 7) // Not on h-file
                    whiteAttacks |= Bitboard.Mask(square + 9); // Up-right
            }
            WhitePawnAttacks[i] = whiteAttacks;

            Bitboard blackAttacks = 0;
            if (fromRank > 0) // Not on 1st rank
            {
                if (fromFile > 0) // Not on a-file
                    blackAttacks |= Bitboard.Mask(square - 9); // Down-left

                if (fromFile < 7) // Not on h-file
                    blackAttacks |= Bitboard.Mask(square - 7); // Down-right
            }
            BlackPawnAttacks[i] = blackAttacks;
        }
    }
}
