using System.Numerics;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public static class AttackGeneration
{
    public static Bitboard CalculateAttacks(Position position, bool forWhite)
    {
        Bitboard attacks = 0;

        // Calculate pawn attacks
        var pawns = forWhite ? position.WhitePawns : position.BlackPawns;
        var pawnAttackTable = forWhite ? AttackTables.WhitePawnAttacks : AttackTables.BlackPawnAttacks;
        attacks |= CalculatePawnAttacksFromTable(pawns, pawnAttackTable);

        // Calculate knight attacks
        var knights = forWhite ? position.WhiteKnights : position.BlackKnights;
        attacks |= CalculateKnightAttacksFromTable(knights);

        // Calculate bishop/queen diagonal attacks
        var bishopsQueens = forWhite ?
            position.WhiteBishops | position.WhiteQueens :
            position.BlackBishops | position.BlackQueens;
        attacks |= CalculateDiagonalAttacks(position, bishopsQueens);

        // Calculate rook/queen straight attacks
        var rooksQueens = forWhite ?
            position.WhiteRooks | position.WhiteQueens :
            position.BlackRooks | position.BlackQueens;
        attacks |= CalculateOrthogonalAttacks(position, rooksQueens);

        // Calculate king attacks
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
        // Use the pre-computed table
        return CalculateKnightAttacksFromTable(knights);
    }

    public static Bitboard CalculateKingAttacks(Bitboard king)
    {
        // Use the pre-computed table
        if (king == 0) return 0;
        var square = king.GetFirstSquare();
        return AttackTables.KingAttacks[(int)square];
    }

    public static Bitboard CalculateDiagonalAttacks(Position position, Bitboard diagonalRayAttackers)
    {
        Bitboard attacks = 0;

        while (diagonalRayAttackers != 0)
        {
            var pieceSquare = BitOperations.TrailingZeroCount(diagonalRayAttackers);
            var pieceFile = pieceSquare % 8;
            var pieceRank = pieceSquare / 8;

            // Four diagonal directions: NE, SE, SW, NW
            Span<int> fileDirections = [1, 1, -1, -1];
            Span<int> rankDirections = [1, -1, -1, 1];

            for (var dir = 0; dir < 4; dir++)
            {
                var currentFile = pieceFile + fileDirections[dir];
                var currentRank = pieceRank + rankDirections[dir];

                while (currentFile is >= 0 and < 8 && currentRank is >= 0 and < 8)
                {
                    var currentSquare = currentRank * 8 + currentFile;
                    var squareMask = Bitboard.Mask(currentSquare);

                    attacks |= squareMask;

                    if ((position.AllPieces & squareMask) != 0)
                    {
                        break;
                    }

                    currentFile += fileDirections[dir];
                    currentRank += rankDirections[dir];
                }
            }

            diagonalRayAttackers &= diagonalRayAttackers - 1;
        }

        return attacks;
    }

    public static Bitboard CalculateOrthogonalAttacks(Position position, Bitboard orthogonalRayAttackers)
    {
        Bitboard attacks = 0;

        while (orthogonalRayAttackers != 0)
        {
            var pieceSquare = BitOperations.TrailingZeroCount(orthogonalRayAttackers);
            var pieceFile = pieceSquare % 8;
            var pieceRank = pieceSquare / 8;

            // Four orthogonal directions: N, E, S, W
            Span<int> fileDirections = [0, 1, 0, -1];
            Span<int> rankDirections = [1, 0, -1, 0];

            for (var dir = 0; dir < 4; dir++)
            {
                var currentFile = pieceFile + fileDirections[dir];
                var currentRank = pieceRank + rankDirections[dir];

                while (currentFile is >= 0 and < 8 && currentRank is >= 0 and < 8)
                {
                    var currentSquare = currentRank * 8 + currentFile;
                    var squareMask = Bitboard.Mask(currentSquare);

                    attacks |= squareMask;

                    if ((position.AllPieces & squareMask) != 0)
                    {
                        break;
                    }

                    currentFile += fileDirections[dir];
                    currentRank += rankDirections[dir];
                }
            }

            orthogonalRayAttackers &= orthogonalRayAttackers - 1;
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
