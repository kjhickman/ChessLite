using System.Numerics;
using System.Runtime.CompilerServices;
using Zugfish.Engine.Extensions;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

/// <summary>
/// Utility class to find suitable magic numbers for magic bitboards.
/// </summary>
public static class MagicNumberFinder
{
    // Random number generator with a fixed seed for reproducibility
    private static readonly Random Random = new Random(42);

    // Default index bits (can be adjusted if needed)
    private static readonly byte[] DefaultBishopIndexBits = new byte[64]
    {
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
    };

    private static readonly byte[] DefaultRookIndexBits = new byte[64]
    {
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12
    };

    /// <summary>
    /// Finds magic numbers for all squares for bishops and rooks.
    /// </summary>
    /// <param name="outputToConsole">Whether to output the magic numbers to the console</param>
    /// <returns>Tuple containing arrays of magic numbers for bishops and rooks</returns>
    public static (ulong[] bishopMagics, ulong[] rookMagics) FindAllMagicNumbers(bool outputToConsole = true)
    {
        ulong[] bishopMagics = new ulong[64];
        ulong[] rookMagics = new ulong[64];

        Console.WriteLine("Finding magic numbers for bishops...");
        for (int square = 0; square < 64; square++)
        {
            bishopMagics[square] = FindMagicNumber((Square)square, true, DefaultBishopIndexBits[square]);
            if (outputToConsole)
            {
                Console.WriteLine($"Bishop square {(Square)square}: 0x{bishopMagics[square]:X16}UL,");
            }
        }

        Console.WriteLine("\nFinding magic numbers for rooks...");
        for (int square = 0; square < 64; square++)
        {
            rookMagics[square] = FindMagicNumber((Square)square, false, DefaultRookIndexBits[square]);
            if (outputToConsole)
            {
                Console.WriteLine($"Rook square {(Square)square}: 0x{rookMagics[square]:X16}UL,");
            }
        }

        // Output the magic numbers in a format that can be directly copied into code
        if (outputToConsole)
        {
            Console.WriteLine("\nBishop magic numbers array:");
            Console.WriteLine("private static readonly ulong[] BishopMagics = new ulong[64]");
            Console.WriteLine("{");
            for (int i = 0; i < 64; i += 4)
            {
                Console.Write("    ");
                for (int j = 0; j < 4 && i + j < 64; j++)
                {
                    Console.Write($"0x{bishopMagics[i + j]:X16}UL, ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("};");

            Console.WriteLine("\nRook magic numbers array:");
            Console.WriteLine("private static readonly ulong[] RookMagics = new ulong[64]");
            Console.WriteLine("{");
            for (int i = 0; i < 64; i += 4)
            {
                Console.Write("    ");
                for (int j = 0; j < 4 && i + j < 64; j++)
                {
                    Console.Write($"0x{rookMagics[i + j]:X16}UL, ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("};");
        }

        return (bishopMagics, rookMagics);
    }

    /// <summary>
    /// Finds a magic number for a specific square and piece type.
    /// </summary>
    /// <param name="square">The square</param>
    /// <param name="isBishop">Whether the piece is a bishop (true) or rook (false)</param>
    /// <param name="bits">Number of bits needed for the index</param>
    /// <returns>A suitable magic number</returns>
    public static ulong FindMagicNumber(Square square, bool isBishop, int bits)
    {
        // Generate mask for the square
        Bitboard mask = isBishop ? GenerateBishopMask(square) : GenerateRookMask(square);

        // Calculate the number of bits set in the mask
        int maskBits = BitOperations.PopCount((ulong)mask);

        // Calculate the size of the lookup table
        int tableSize = 1 << maskBits;

        // Generate all possible blocker configurations and their corresponding attacks
        Bitboard[] blockers = new Bitboard[tableSize];
        Bitboard[] attacks = new Bitboard[tableSize];

        // Generate reference attacks for verification
        for (int i = 0; i < tableSize; i++)
        {
            blockers[i] = GenerateBlockers(i, mask);
            attacks[i] = isBishop
                ? GenerateBishopAttacksOnTheFly(square, blockers[i])
                : GenerateRookAttacksOnTheFly(square, blockers[i]);
        }

        // The shift amount for the magic multiplication
        int shift = 64 - bits;

        // Try magic numbers until a suitable one is found
        for (int attempt = 0; attempt < 100000000; attempt++)
        {
            // Generate a candidate magic number
            ulong magic = GenerateRandomMagicCandidate();

            // Skip if the magic number doesn't meet basic quality criteria
            if (BitOperations.PopCount((mask * magic) & 0xFF00000000000000) < 6)
                continue;

            // Test this magic number
            if (TryMagicNumber(magic, shift, mask, blockers, attacks, tableSize))
            {
                return magic;
            }
        }

        throw new InvalidOperationException($"Failed to find a magic number for {(isBishop ? "bishop" : "rook")} at square {square}");
    }

    /// <summary>
    /// Tests if a magic number produces no collisions.
    /// </summary>
    private static bool TryMagicNumber(ulong magic, int shift, Bitboard mask, Bitboard[] blockers,
        Bitboard[] attacks, int tableSize)
    {
        // Initialize a used table to track which indices have been used
        Bitboard[] used = new Bitboard[1 << shift];
        Bitboard[] reference = new Bitboard[1 << shift];

        Array.Fill(used, 0UL);

        // Check for collisions
        for (int i = 0; i < tableSize; i++)
        {
            // Calculate the magic index
            int index = (int)(((ulong)(blockers[i] * magic)) >> shift);

            // If this index is already used with a different attack pattern, there's a collision
            if (used[index] != 0 && reference[index] != attacks[i])
            {
                return false;
            }

            // Mark this index as used
            used[index] = 1;
            reference[index] = attacks[i];
        }

        // No collisions found
        return true;
    }

    /// <summary>
    /// Generates a random 64-bit value with few bits set (suitable as a magic candidate).
    /// </summary>
    private static ulong GenerateRandomMagicCandidate()
    {
        return (ulong)Random.NextInt64() & (ulong)Random.NextInt64() & (ulong)Random.NextInt64();
    }

    /// <summary>
    /// Generates all possible blockers for a given mask based on an index.
    /// </summary>
    private static Bitboard GenerateBlockers(int index, Bitboard mask)
    {
        Bitboard blockers = 0;
        int bits = BitOperations.PopCount((ulong)mask);
        for (int i = 0; i < bits; i++)
        {
            int bitPos = BitOperations.TrailingZeroCount(mask);
            mask &= mask - 1; // Clear the least significant bit

            if ((index & (1 << i)) != 0)
            {
                blockers |= 1UL << bitPos;
            }
        }
        return blockers;
    }

    /// <summary>
    /// Generates bishop attack mask (relevant squares only).
    /// </summary>
    private static Bitboard GenerateBishopMask(Square square)
    {
        Bitboard mask = 0;
        int rank = square.GetRank();
        int file = square.GetFile();

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

    /// <summary>
    /// Generates rook attack mask (relevant squares only).
    /// </summary>
    private static Bitboard GenerateRookMask(Square square)
    {
        Bitboard mask = 0;
        int rank = square.GetRank();
        int file = square.GetFile();

        // Horizontal and vertical rays, stopping one square from the edge
        for (int r = rank + 1; r < 7; r++)
            mask |= 1UL << (r * 8 + file);
        for (int r = rank - 1; r > 0; r--)
            mask |= 1UL << (r * 8 + file);
        for (int f = file + 1; f < 7; f++)
            mask |= 1UL << (rank * 8 + f);
        for (int f = file - 1; f > 0; f--)
            mask |= 1UL << (rank * 8 + f);

        return mask;
    }

    /// <summary>
    /// Generate bishop attacks on the fly for a given square and blocker configuration.
    /// </summary>
    private static Bitboard GenerateBishopAttacksOnTheFly(Square square, Bitboard blockers)
    {
        Bitboard attacks = 0;
        int rank = square.GetRank();
        int file = square.GetFile();

        // North-East
        for (int r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South-East
        for (int r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South-West
        for (int r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // North-West
        for (int r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
        {
            Bitboard bit = 1UL << (r * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }

    /// <summary>
    /// Generate rook attacks on the fly for a given square and blocker configuration.
    /// </summary>
    private static Bitboard GenerateRookAttacksOnTheFly(Square square, Bitboard blockers)
    {
        Bitboard attacks = 0;
        int rank = square.GetRank();
        int file = square.GetFile();

        // North
        for (int r = rank + 1; r < 8; r++)
        {
            Bitboard bit = 1UL << (r * 8 + file);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // East
        for (int f = file + 1; f < 8; f++)
        {
            Bitboard bit = 1UL << (rank * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // South
        for (int r = rank - 1; r >= 0; r--)
        {
            Bitboard bit = 1UL << (r * 8 + file);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        // West
        for (int f = file - 1; f >= 0; f--)
        {
            Bitboard bit = 1UL << (rank * 8 + f);
            attacks |= bit;
            if ((blockers & bit) != 0) break;
        }

        return attacks;
    }

    /// <summary>
    /// Creates a test driver program to find and validate magic numbers
    /// </summary>
    public static void CreateTestDriver()
    {
        (ulong[] bishopMagics, ulong[] rookMagics) = FindAllMagicNumbers(true);

        // Validate the magic numbers
        Console.WriteLine("\nValidating magic numbers...");
        bool allValid = true;

        for (int square = 0; square < 64; square++)
        {
            // Validate bishop magic
            if (!ValidateMagicNumber((Square)square, true, bishopMagics[square], DefaultBishopIndexBits[square]))
            {
                Console.WriteLine($"Invalid bishop magic for square {(Square)square}");
                allValid = false;
            }

            // Validate rook magic
            if (!ValidateMagicNumber((Square)square, false, rookMagics[square], DefaultRookIndexBits[square]))
            {
                Console.WriteLine($"Invalid rook magic for square {(Square)square}");
                allValid = false;
            }
        }

        if (allValid)
        {
            Console.WriteLine("All magic numbers are valid!");
        }

        // Output the magic numbers in the requested format for easy copy-paste
        Console.WriteLine("\nFormatted output for direct use in code:");

        // Bishop magics
        Console.Write("bishop magics: [");
        for (int i = 0; i < 64; i++)
        {
            Console.Write($"0x{bishopMagics[i]:X16}UL");
            if (i < 63) Console.Write(", ");
        }
        Console.WriteLine("]");

        // Rook magics
        Console.Write("rook magics: [");
        for (int i = 0; i < 64; i++)
        {
            Console.Write($"0x{rookMagics[i]:X16}UL");
            if (i < 63) Console.Write(", ");
        }
        Console.WriteLine("]");
    }

    /// <summary>
    /// Validates a magic number for a specific square and piece type.
    /// </summary>
    private static bool ValidateMagicNumber(Square square, bool isBishop, ulong magic, int bits)
    {
        // Generate mask for the square
        Bitboard mask = isBishop ? GenerateBishopMask(square) : GenerateRookMask(square);

        // Calculate the number of bits set in the mask
        int maskBits = BitOperations.PopCount((ulong)mask);

        // Calculate the size of the lookup table
        int tableSize = 1 << maskBits;

        // Generate all possible blocker configurations and their corresponding attacks
        Bitboard[] blockers = new Bitboard[tableSize];
        Bitboard[] attacks = new Bitboard[tableSize];

        // Generate reference attacks for verification
        for (int i = 0; i < tableSize; i++)
        {
            blockers[i] = GenerateBlockers(i, mask);
            attacks[i] = isBishop
                ? GenerateBishopAttacksOnTheFly(square, blockers[i])
                : GenerateRookAttacksOnTheFly(square, blockers[i]);
        }

        // The shift amount for the magic multiplication
        int shift = 64 - bits;

        // Test the magic number
        return TryMagicNumber(magic, shift, mask, blockers, attacks, tableSize);
    }
}
