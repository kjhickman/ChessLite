using System.Numerics;
using System.Runtime.CompilerServices;
using Zugfish.Engine.Extensions;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public static class MagicBitboards
{
    private static readonly ulong[] BishopMagics =
    [
        0x0020520412418200UL, 0x4110425204002011UL, 0x0004484083010400UL, 0x00080A0822088805UL,
        0x0004242000000050UL, 0x001101084008000AUL, 0x0400461610402000UL, 0x0004140402021000UL,
        0x5504841150120480UL, 0x0200200202006308UL, 0x000C414216004000UL, 0x0020440408820210UL,
        0x1004011140210000UL, 0x4822121210440202UL, 0x20E8405202104004UL, 0x1011402101082010UL,
        0x0142000498083110UL, 0x047102090C830C00UL, 0x0104017218001100UL, 0x012420480200A420UL,
        0x0482001012100000UL, 0x0002044300420A00UL, 0x0004040100821000UL, 0x0000804022080210UL,
        0x1089A00304441000UL, 0x20082A0414E40820UL, 0x0004480005020404UL, 0x0210040000401020UL,
        0x2281010000104002UL, 0x0834030008090140UL, 0x2002006100881800UL, 0x1211004001084802UL,
        0x0202022352400800UL, 0x5848120904020811UL, 0x0080405000080820UL, 0x2814A08020280200UL,
        0x5090620020060080UL, 0x0102081110020040UL, 0x0202020624404800UL, 0x08010C1104448040UL,
        0x1005380824044100UL, 0x2804050130621800UL, 0x0401002D01001000UL, 0x0460002011040810UL,
        0x0020284904000841UL, 0x0070010101000808UL, 0x189030014100B040UL, 0x00020C0061822602UL,
        0x0101080202201000UL, 0x0021040159080804UL, 0x0450060101211800UL, 0x0000100642020401UL,
        0x40000C10420E0800UL, 0x010120A022408801UL, 0x0009021004010810UL, 0x4120488080808588UL,
        0x1040440228010407UL, 0x0081902402421005UL, 0x0444006100809000UL, 0x000000030210440CUL,
        0x4102702840104122UL, 0x0200024010140930UL, 0x000220880240C404UL, 0x0004480808042044UL
    ];

    private static readonly ulong[] RookMagics =
    [
        0x20800014A0400A80UL, 0x0040100020004000UL, 0x5080088010002000UL, 0x0100042100081000UL,
        0x0280140008000280UL, 0x0200089004212200UL, 0x0080020001000080UL, 0x0100010009423082UL,
        0x0400800192254006UL, 0x2023002110400082UL, 0x0012001200408028UL, 0x090A000920104204UL,
        0x00110011000800C4UL, 0x0000808002000400UL, 0x0909001200110014UL, 0x0042002084004609UL,
        0x1840818002400021UL, 0x0090024002200048UL, 0x4890002008040020UL, 0x0012020008104020UL,
        0x1000808004000800UL, 0x0001010002040008UL, 0x0090040002411008UL, 0x003106000080540DUL,
        0x1020902080004004UL, 0x0481200480400280UL, 0x0000200080801000UL, 0x58002042000A0210UL,
        0x1484008080080006UL, 0x0000040080020080UL, 0x0018080400011002UL, 0x52000502000048A4UL,
        0x1000400020801080UL, 0x0800804000802000UL, 0x0000100084802000UL, 0x0230040041402800UL,
        0x00020050E2000804UL, 0x4082000502000810UL, 0x0002005142000824UL, 0x1800008402000041UL,
        0x0020800040008020UL, 0x4040400100810028UL, 0x04C0200100410018UL, 0x0580400822020010UL,
        0x0400080004008080UL, 0x0110020004008080UL, 0x0001002200110004UL, 0x0800104A8B02000CUL,
        0x0080002000400040UL, 0x000C229042010200UL, 0x0001A00080100180UL, 0x0111080083100080UL,
        0x0202000804102200UL, 0x0010800400020080UL, 0x0402011088020400UL, 0x0000041105418A00UL,
        0x0002205141060482UL, 0x4068420015008122UL, 0x0000801022000842UL, 0x3401000820041001UL,
        0x00A9000800900205UL, 0x0019000234001841UL, 0x0040121088011004UL, 0x0420202444008106UL
    ];

    // Bits needed for the index in the attack table
    private static readonly byte[] BishopIndexBits =
    [
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
    ];

    private static readonly byte[] RookIndexBits =
    [
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12
    ];

    private static readonly Bitboard[] BishopMasks = new Bitboard[64];
    private static readonly Bitboard[] RookMasks = new Bitboard[64];
    private static readonly Bitboard[][] BishopAttacks;
    private static readonly Bitboard[][] RookAttacks;
    private static readonly byte[] BishopShifts = new byte[64];
    private static readonly byte[] RookShifts = new byte[64];

    static MagicBitboards()
    {
        // Calculate shifts
        for (var i = 0; i < 64; i++)
        {
            BishopShifts[i] = (byte)(64 - BishopIndexBits[i]);
            RookShifts[i] = (byte)(64 - RookIndexBits[i]);
        }

        InitializeMasks();
        BishopAttacks = new Bitboard[64][];
        RookAttacks = new Bitboard[64][];

        for (var square = 0; square < 64; square++)
        {
            // Allocate attack tables based on the number of index bits needed
            BishopAttacks[square] = new Bitboard[1 << BishopIndexBits[square]];
            RookAttacks[square] = new Bitboard[1 << RookIndexBits[square]];

            FillAttackTable(square, BishopMasks[square], BishopMagics[square], BishopShifts[square], BishopAttacks[square], true);
            FillAttackTable(square, RookMasks[square], RookMagics[square], RookShifts[square], RookAttacks[square], false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard GetBishopAttacks(Square square, Bitboard blockers)
    {
        var squareIndex = (int)square;
        var relevantBlockers = blockers & BishopMasks[squareIndex];
        var index = (int)((relevantBlockers * BishopMagics[squareIndex]) >> BishopShifts[squareIndex]);
        return BishopAttacks[squareIndex][index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard GetRookAttacks(Square square, Bitboard blockers)
    {
        var squareIndex = (int)square;
        var relevantBlockers = blockers & RookMasks[squareIndex];
        var index = (int)((relevantBlockers * RookMagics[squareIndex]) >> RookShifts[squareIndex]);
        return RookAttacks[squareIndex][index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard GetQueenAttacks(Square square, Bitboard blockers)
    {
        return GetBishopAttacks(square, blockers) | GetRookAttacks(square, blockers);
    }

    private static void InitializeMasks()
    {
        for (var square = 0; square < 64; square++)
        {
            BishopMasks[square] = GenerateBishopMask((Square)square);
            RookMasks[square] = GenerateRookMask((Square)square);
        }
    }

    private static void FillAttackTable(int square, Bitboard mask, ulong magic, byte shift, Bitboard[] table, bool isBishop)
    {
        var bitCount = mask.Count();
        var tableSize = 1 << bitCount;

        for (var index = 0; index < tableSize; index++)
        {
            var blockers = GenerateBlockers(index, mask);

            var attacks = isBishop
                ? GenerateBishopAttacksOnTheFly((Square)square, blockers)
                : GenerateRookAttacksOnTheFly((Square)square, blockers);

            var magicIndex = (int)((blockers * magic) >> shift);

            table[magicIndex] = attacks;
        }
    }

    private static Bitboard GenerateBlockers(int index, Bitboard mask)
    {
        Bitboard blockers = 0;
        var bits = mask.Count();
        for (var i = 0; i < bits; i++)
        {
            var bitPos = BitOperations.TrailingZeroCount(mask);
            mask &= mask - 1; // Clear the least significant bit

            if ((index & (1 << i)) != 0)
            {
                blockers |= 1UL << bitPos;
            }
        }
        return blockers;
    }

    private static Bitboard GenerateBishopMask(Square square)
    {
        Bitboard mask = 0;
        var rank = square.GetRank();
        var file = square.GetFile();

        // Diagonal rays in all 4 directions, stopping one square from the edge
        for (int r = rank + 1, f = file + 1; r < 7 && f < 7; r++, f++)
            mask |= 1UL << (r * 8 + f);
        for (int r = rank + 1, f = file - 1; r < 7 && f > 0; r++, f--)
            mask |= 1UL << (r * 8 + f);
        for (int r = rank - 1, f = file + 1; r > 0 && f < 7; r--, f++)
            mask |= 1UL << (r * 8 + f);
        for (int r = rank - 1, f = file - 1; r > 0 && f > 0; r--, f--)
            mask |= 1UL << (r * 8 + f);

        return mask;
    }

    private static Bitboard GenerateRookMask(Square square)
    {
        Bitboard mask = 0;
        var rank = square.GetRank();
        var file = square.GetFile();

        // Horizontal and vertical rays, stopping one square from the edge
        for (var r = rank + 1; r < 7; r++)
            mask |= 1UL << (r * 8 + file);
        for (var r = rank - 1; r > 0; r--)
            mask |= 1UL << (r * 8 + file);
        for (var f = file + 1; f < 7; f++)
            mask |= 1UL << (rank * 8 + f);
        for (var f = file - 1; f > 0; f--)
            mask |= 1UL << (rank * 8 + f);

        return mask;
    }

    private static Bitboard GenerateBishopAttacksOnTheFly(Square square, Bitboard blockers)
    {
        Bitboard attacks = 0;
        var rank = square.GetRank();
        var file = square.GetFile();
        int r, f;

        // Northeast
        for (r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // Northwest
        for (r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // Southeast
        for (r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // Southwest
        for (r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }

    private static Bitboard GenerateRookAttacksOnTheFly(Square square, Bitboard blockers)
    {
        Bitboard attacks = 0;
        var rank = square.GetRank();
        var file = square.GetFile();

        // North
        for (var r = rank + 1; r < 8; r++)
        {
            Bitboard bit = 1UL << (r * 8 + file);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South
        for (var r = rank - 1; r >= 0; r--)
        {
            Bitboard bit = 1UL << (r * 8 + file);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // East
        for (var f = file + 1; f < 8; f++)
        {
            Bitboard bit = 1UL << (rank * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // West
        for (var f = file - 1; f >= 0; f--)
        {
            Bitboard bit = 1UL << (rank * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }
}
