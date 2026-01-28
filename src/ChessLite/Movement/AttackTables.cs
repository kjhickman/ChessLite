using ChessLite.Primitives;

namespace ChessLite.Movement;

internal static class AttackTables
{
    internal static readonly Bitboard[] KnightAttacks = new Bitboard[64];
    internal static readonly Bitboard[] KingAttacks = new Bitboard[64];
    internal static readonly Bitboard[] WhitePawnAttacks = new Bitboard[64];
    internal static readonly Bitboard[] BlackPawnAttacks = new Bitboard[64];

    /// <summary>
    /// Precomputed rays between any two squares. RayBetween[from][to] contains
    /// all squares strictly between 'from' and 'to' (exclusive of both endpoints).
    /// Returns 0 if squares are not aligned on a rank, file, or diagonal.
    /// </summary>
    internal static readonly Bitboard[][] RayBetween = new Bitboard[64][];

    static AttackTables()
    {
        InitializeKnightAttacks();
        InitializeKingAttacks();
        InitializePawnAttacks();
        InitializeRayBetween();
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

    private static void InitializeRayBetween()
    {
        for (var from = 0; from < 64; from++)
        {
            RayBetween[from] = new Bitboard[64];
            var fromFile = from % 8;
            var fromRank = from / 8;

            for (var to = 0; to < 64; to++)
            {
                if (from == to)
                {
                    RayBetween[from][to] = 0;
                    continue;
                }

                var toFile = to % 8;
                var toRank = to / 8;
                var fileDelta = toFile - fromFile;
                var rankDelta = toRank - fromRank;

                // Check if squares are aligned (same rank, file, or diagonal)
                var isOrthogonal = fileDelta == 0 || rankDelta == 0;
                var isDiagonal = Math.Abs(fileDelta) == Math.Abs(rankDelta);

                if (!isOrthogonal && !isDiagonal)
                {
                    RayBetween[from][to] = 0;
                    continue;
                }

                // Calculate direction
                var fileStep = fileDelta == 0 ? 0 : fileDelta > 0 ? 1 : -1;
                var rankStep = rankDelta == 0 ? 0 : rankDelta > 0 ? 1 : -1;
                var squareStep = rankStep * 8 + fileStep;

                // Build the ray between (exclusive of endpoints)
                Bitboard ray = 0;
                var current = from + squareStep;
                while (current != to)
                {
                    ray |= 1UL << current;
                    current += squareStep;
                }

                RayBetween[from][to] = ray;
            }
        }
    }
}
