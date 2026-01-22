using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Movement;

internal static class AttackGeneration
{
    internal static Bitboard CalculateAttacks(Position position, bool forWhite)
    {
        Bitboard attacks = 0;

        var pawns = forWhite ? position.WhitePawns : position.BlackPawns;
        attacks |= CalculatePawnAttacks(pawns, forWhite);

        var knights = forWhite ? position.WhiteKnights : position.BlackKnights;
        attacks |= CalculateKnightAttacks(knights);

        var bishops = forWhite ? position.WhiteBishops : position.BlackBishops;
        attacks |= CalculateBishopAttacks(bishops, position.AllPieces);

        var rooks = forWhite ? position.WhiteRooks : position.BlackRooks;
        attacks |= CalculateRookAttacks(rooks, position.AllPieces);

        var queens = forWhite ? position.WhiteQueens : position.BlackQueens;
        attacks |= CalculateQueenAttacks(queens, position.AllPieces);

        var king = forWhite ? position.WhiteKing : position.BlackKing;
        attacks |= CalculateKingAttacks(king);

        return attacks;
    }

    internal static Bitboard CalculateAttacksWithoutOpposingKing(Position position, bool forWhite)
    {
        Bitboard attacks = 0;

        var pawns = forWhite ? position.WhitePawns : position.BlackPawns;
        attacks |= CalculatePawnAttacks(pawns, forWhite);

        var knights = forWhite ? position.WhiteKnights : position.BlackKnights;
        attacks |= CalculateKnightAttacks(knights);

        var kingMask = forWhite ? position.BlackKing : position.WhiteKing;
        var bishops = forWhite ? position.WhiteBishops : position.BlackBishops;
        attacks |= CalculateBishopAttacks(bishops, position.AllPieces.ClearSquares(kingMask));

        var rooks = forWhite ? position.WhiteRooks : position.BlackRooks;
        attacks |= CalculateRookAttacks(rooks, position.AllPieces.ClearSquares(kingMask));

        var queens = forWhite ? position.WhiteQueens : position.BlackQueens;
        attacks |= CalculateQueenAttacks(queens, position.AllPieces.ClearSquares(kingMask));

        var king = forWhite ? position.WhiteKing : position.BlackKing;
        attacks |= CalculateKingAttacks(king);

        return attacks;
    }

    internal static Bitboard CalculatePawnAttacks(Bitboard pawns, bool forWhite)
    {
        var attackTable = forWhite ? AttackTables.WhitePawnAttacks : AttackTables.BlackPawnAttacks;
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

    internal static Bitboard CalculateKnightAttacks(Bitboard knights)
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

    internal static Bitboard CalculateKingAttacks(Bitboard king)
    {
        if (king.IsEmpty()) return 0;
        var square = king.GetFirstSquare();
        return AttackTables.KingAttacks[(int)square];
    }

    internal static Bitboard CalculateBishopAttacks(Bitboard bishops, Bitboard allPieces)
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

    internal static Bitboard CalculateRookAttacks(Bitboard rooks, Bitboard allPieces)
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

    internal static Bitboard CalculateQueenAttacks(Bitboard queens, Bitboard allPieces)
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
}
