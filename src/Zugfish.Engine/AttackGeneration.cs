using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public static class AttackGeneration
{
    public static Bitboard CalculateAttacks(Position position, bool forWhite)
    {
        Bitboard attacks = 0;

        var pawns = forWhite ? position.WhitePawns : position.BlackPawns;
        var pawnAttackTable = forWhite ? AttackTables.WhitePawnAttacks : AttackTables.BlackPawnAttacks;
        attacks |= CalculatePawnAttacksFromTable(pawns, pawnAttackTable);

        var knights = forWhite ? position.WhiteKnights : position.BlackKnights;
        attacks |= CalculateKnightAttacksFromTable(knights);

        var bishops = forWhite ? position.WhiteBishops : position.BlackBishops;
        attacks |= CalculateBishopAttacks(bishops, position.AllPieces);

        var rooks = forWhite ? position.WhiteRooks : position.BlackRooks;
        attacks |= CalculateRookAttacks(rooks, position.AllPieces);

        var queens = forWhite ? position.WhiteQueens : position.BlackQueens;
        attacks |= CalculateQueenAttacks(queens, position.AllPieces);

        var king = forWhite ? position.WhiteKing : position.BlackKing;
        attacks |= CalculateKingAttacksFromTable(king);

        return attacks;
    }

    public static Bitboard CalculatePawnAttacks(Bitboard pawns, bool forWhite)
    {
        // Use the pre-computed tables
        var attackTable = forWhite ? AttackTables.WhitePawnAttacks : AttackTables.BlackPawnAttacks;
        return CalculatePawnAttacksFromTable(pawns, attackTable);
    }

    public static Bitboard CalculateKnightAttacks(Bitboard knights)
    {
        return CalculateKnightAttacksFromTable(knights);
    }

    public static Bitboard CalculateKingAttacks(Bitboard king)
    {
        if (king == 0) return 0;
        var square = king.GetFirstSquare();
        return AttackTables.KingAttacks[(int)square];
    }

    public static Bitboard CalculateBishopAttacks(Bitboard bishops, Bitboard allPieces)
    {
        Bitboard attacks = 0;
        var currentBishops = bishops;

        while (currentBishops.IsNotEmpty())
        {
            var square = currentBishops.GetFirstSquare();
            attacks |= MagicBitboards.GetBishopAttacks(square, allPieces);
            currentBishops &= currentBishops - 1; // Clear the least significant bit
        }

        return attacks;
    }

    public static Bitboard CalculateRookAttacks(Bitboard rooks, Bitboard allPieces)
    {
        Bitboard attacks = 0;
        var currentRooks = rooks;

        while (currentRooks.IsNotEmpty())
        {
            var square = currentRooks.GetFirstSquare();
            attacks |= MagicBitboards.GetRookAttacks(square, allPieces);
            currentRooks &= currentRooks - 1; // Clear the least significant bit
        }

        return attacks;
    }

    public static Bitboard CalculateQueenAttacks(Bitboard queens, Bitboard allPieces)
    {
        Bitboard attacks = 0;
        var currentQueens = queens;

        while (currentQueens.IsNotEmpty())
        {
            var square = currentQueens.GetFirstSquare();
            attacks |= MagicBitboards.GetQueenAttacks(square, allPieces);
            currentQueens &= currentQueens - 1; // Clear the least significant bit
        }

        return attacks;
    }

    private static Bitboard CalculatePawnAttacksFromTable(Bitboard pawns, Bitboard[] attackTable)
    {
        Bitboard attacks = 0;
        var currentPawns = pawns;

        while (currentPawns.IsNotEmpty())
        {
            var square = currentPawns.GetFirstSquare();
            attacks |= attackTable[(int)square];
            currentPawns &= currentPawns - 1; // Clear the least significant bit
        }

        return attacks;
    }

    private static Bitboard CalculateKnightAttacksFromTable(Bitboard knights)
    {
        Bitboard attacks = 0;
        var currentKnights = knights;

        while (currentKnights.IsNotEmpty())
        {
            var square = currentKnights.GetFirstSquare();
            attacks |= AttackTables.KnightAttacks[(int)square];
            currentKnights &= currentKnights - 1; // Clear the least significant bit
        }

        return attacks;
    }

    private static Bitboard CalculateKingAttacksFromTable(Bitboard king)
    {
        if (king == 0) return 0;
        var square = king.GetFirstSquare();
        return AttackTables.KingAttacks[(int)square];
    }
}
