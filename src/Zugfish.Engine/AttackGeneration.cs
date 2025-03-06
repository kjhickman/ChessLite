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
        attacks |= CalculatePawnAttacks(pawns, forWhite);

        // Calculate knight attacks
        var knights = forWhite ? position.WhiteKnights : position.BlackKnights;
        attacks |= CalculateKnightAttacks(knights);

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
        attacks |= CalculateKingAttacks(king);

        return attacks;
    }

    public static Bitboard CalculatePawnAttacks(Bitboard pawns, bool isWhite)
    {
        Bitboard attacks;
        if (isWhite)
        {
            var upLeftAttacks = (pawns << 7) & ~Constants.FileH; // Up-left: shift northeast and mask off H-file
            var upRightAttacks = (pawns << 9) & ~Constants.FileA; // Up-right: shift northwest and mask off A-file
            attacks = upLeftAttacks | upRightAttacks;
        }
        else
        {
            var downLeftAttacks = (pawns >> 9) & ~Constants.FileH; // Down-left: shift southeast and mask off h-file
            var downRightAttacks = (pawns >> 7) & ~Constants.FileA; // Down-right: shift southwest and mask off a-file
            attacks = downLeftAttacks | downRightAttacks;
        }

        return attacks;
    }

    public static Bitboard CalculateKnightAttacks(Bitboard knights)
    {
        Bitboard attacks = 0;

        while (knights != 0)
        {
            var knightSquare = BitOperations.TrailingZeroCount(knights);
            var knightFile = knightSquare % 8;
            var knightRank = knightSquare / 8;

            // Knight's 8 possible moves
            Span<int> fileOffsets = [-2, -2, -1, -1, 1, 1, 2, 2];
            Span<int> rankOffsets = [-1, 1, -2, 2, -2, 2, -1, 1];

            for (var i = 0; i < 8; i++)
            {
                var targetFile = knightFile + fileOffsets[i];
                var targetRank = knightRank + rankOffsets[i];

                // Check if target square is on the board
                if (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
                {
                    var targetSquare = targetRank * 8 + targetFile;
                    attacks |= Bitboard.Mask(targetSquare);
                }
            }

            knights &= knights - 1;
        }

        return attacks;
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

    public static Bitboard CalculateKingAttacks(Bitboard king)
    {
        if (king == 0) return 0;

        var kingSquare = BitOperations.TrailingZeroCount(king);
        var kingFile = kingSquare % 8;
        var kingRank = kingSquare / 8;

        Bitboard attacks = 0;

        // King attacks all 8 surrounding squares (-1, 0, 1 for both file and rank)
        for (var fileOffset = -1; fileOffset <= 1; fileOffset++)
        {
            for (var rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                // Skip the king's own square
                if (fileOffset == 0 && rankOffset == 0) continue;

                var targetFile = kingFile + fileOffset;
                var targetRank = kingRank + rankOffset;

                if (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
                {
                    var targetSquare = targetRank * 8 + targetFile;
                    attacks |= Bitboard.Mask(targetSquare);
                }
            }
        }

        return attacks;
    }
}
