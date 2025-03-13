using System.Numerics;
using Veloce.Extensions;
using Veloce.Models;

namespace Veloce;

public static class LegalityChecker
{
    public static bool IsMoveLegal(Position position, Move move)
    {
        var isWhite = position.WhiteToMove;
        var kingSquare = isWhite ? position.WhiteKing.GetFirstSquare() : position.BlackKing.GetFirstSquare();
        var targetSquare = move.To;

        if (move.PieceType is PieceType.WhiteKing or PieceType.BlackKing)
        {
            return !WouldKingBeAttacked(position, targetSquare, kingSquare, !isWhite);
        }

        if (position.IsInCheck())
        {
            var checkingPieces = FindAttackingPieces(position, kingSquare, !isWhite);
            var checkCount = checkingPieces.Count();

            // If double check, only king moves can resolve it
            if (checkCount > 1)
            {
                return false;
            }

            var checkingSquare = checkingPieces.GetFirstSquare();
            if (targetSquare == checkingSquare ||
                (move.SpecialMoveType == SpecialMoveType.EnPassant && targetSquare == checkingSquare + (isWhite ? 8 : -8)))
            {
                // Check if the piece is pinned
                return !IsPiecePinned(position, kingSquare, move);
            }

            var blockingSquares = GetBlockingSquares(position, kingSquare, checkingSquare);
            return blockingSquares.Intersects(Bitboard.Mask(targetSquare)) && !IsPiecePinned(position, kingSquare, move);
        }

        if (IsPiecePinned(position, kingSquare, move))
        {
            return IsMovingAlongPinRay(move, kingSquare);
        }

        return true;
    }

    private static bool WouldKingBeAttacked(Position position, Square kingTargetSquare, Square currentKingSquare, bool byWhite)
    {
        var targetSquareMask = Bitboard.Mask(kingTargetSquare);

        // Check pawn attacks
        var pawnAttacks = byWhite ? position.WhitePawnAttacks : position.BlackPawnAttacks;
        if (targetSquareMask.Intersects(pawnAttacks)) return true;

        // Check knight attacks
        var knightAttacks = byWhite ? position.WhiteKnightAttacks : position.BlackKnightAttacks;
        if (targetSquareMask.Intersects(knightAttacks)) return true;

        // Check king attacks
        var kingAttacks = byWhite ? position.WhiteKingAttacks : position.BlackKingAttacks;
        if (targetSquareMask.Intersects(kingAttacks)) return true;

        // For sliding piece attacks, we need calculate the rays excluding the king

        // Create a modified position bitboard for ray calculation
        var allPiecesExceptKing = position.AllPieces.ClearSquare(currentKingSquare);

        // Check bishop/queen diagonal attacks
        var bishopsQueens = byWhite
            ? position.WhiteBishops | position.WhiteQueens
            : position.BlackBishops | position.BlackQueens;

        if (bishopsQueens != 0)
        {
            var diagonalAttacks = CalculateDiagonalAttacks(allPiecesExceptKing, bishopsQueens);
            if (targetSquareMask.Intersects(diagonalAttacks)) return true;
        }

        // Check rook/queen orthogonal attacks
        var rooksQueens =
            byWhite ? position.WhiteRooks | position.WhiteQueens : position.BlackRooks | position.BlackQueens;

        if (rooksQueens != 0)
        {
            var orthogonalAttacks = CalculateOrthogonalAttacks(allPiecesExceptKing, rooksQueens);
            if (targetSquareMask.Intersects(orthogonalAttacks)) return true;
        }

        return false; // No attacks found
    }

    private static Bitboard CalculateDiagonalAttacks(Bitboard allPiecesExceptKing, Bitboard diagonalRayAttackers)
    {
        Bitboard attacks = 0;

        while (diagonalRayAttackers != 0)
        {
            var pieceSquare = BitOperations.TrailingZeroCount(diagonalRayAttackers);
            attacks |= MagicBitboards.GetBishopAttacks((Square)pieceSquare, allPiecesExceptKing);
            diagonalRayAttackers &= diagonalRayAttackers - 1;
        }

        return attacks;
    }

    private static Bitboard CalculateOrthogonalAttacks(Bitboard allPiecesExceptKing, Bitboard orthogonalRayAttackers)
    {
        Bitboard attacks = 0;

        while (orthogonalRayAttackers != 0)
        {
            var pieceSquare = BitOperations.TrailingZeroCount(orthogonalRayAttackers);
            attacks |= MagicBitboards.GetRookAttacks((Square)pieceSquare, allPiecesExceptKing);
            orthogonalRayAttackers &= orthogonalRayAttackers - 1;
        }

        return attacks;
    }

    private static Bitboard FindAttackingPieces(Position position, Square square, bool forWhite)
    {
        // Pawn attackers
        var possiblePawnAttacks = forWhite
            ? AttackTables.BlackPawnAttacks[(int)square]
            : AttackTables.WhitePawnAttacks[(int)square];
        var pawnAttackers = possiblePawnAttacks & (forWhite ? position.WhitePawns : position.BlackPawns);

        // Knight attackers
        var possibleKnightAttacks = AttackTables.KnightAttacks[(int)square];
        var knightAttackers = possibleKnightAttacks & (forWhite ? position.WhiteKnights : position.BlackKnights);

        // Diagonal attackers
        var diagonalSliders = forWhite
            ? position.WhiteBishops | position.WhiteQueens
            : position.BlackBishops | position.BlackQueens;
        var bishopQueenAttackers = MagicBitboards.GetBishopAttacks(square, position.AllPieces) & diagonalSliders;

        var orthogonalSliders = forWhite
            ? position.WhiteRooks | position.WhiteQueens
            : position.BlackRooks | position.BlackQueens;
        var rookQueenAttackers = MagicBitboards.GetRookAttacks(square, position.AllPieces) & orthogonalSliders;

        return pawnAttackers | knightAttackers | bishopQueenAttackers | rookQueenAttackers;
    }

    private static bool IsPiecePinned(Position position, Square kingSquare, Move move)
    {
        var kingRank = kingSquare.GetRank();
        var pieceRank = move.From.GetRank();
        return move.SpecialMoveType == SpecialMoveType.EnPassant && kingRank == pieceRank
            ? IsEnPassantPinned(position, move, kingSquare)
            : position.PinnedPieces.Intersects(move.From);
    }

    private static bool IsEnPassantPinned(Position position, Move move, Square kingSquare)
    {
        var capturedPawnSquare = position.WhiteToMove
            ? move.To - 8 // White capturing a black pawn (pawn is one rank below destination)
            : move.To + 8; // Black capturing a white pawn (pawn is one rank above destination)

        // For a horizontal pin to be possible, the king and the capturing pawn must be on the same rank
        var kingRank = kingSquare.GetRank();
        var fromRank = move.From.GetRank();

        // If king is not on the same rank as the capturing pawn, no horizontal pin is possible
        if (kingRank != fromRank) return false;

        // Create a bitboard with both the capturing pawn and captured pawn removed
        var allPiecesWithoutPawns = position.AllPieces
            .ClearSquare(move.From)
            .ClearSquare(capturedPawnSquare);

        // Check for enemy rooks/queens
        var enemyRooksQueens = position.WhiteToMove
            ? position.BlackRooks | position.BlackQueens
            : position.WhiteRooks | position.WhiteQueens;

        // No enemy rooks/queens, no pin possible
        if (enemyRooksQueens == 0) return false;

        var kingFile = kingSquare.GetFile();

        // Check for attacking rooks/queens to the left of the king
        for (var file = kingFile - 1; file >= 0; file--)
        {
            var square = (Square)(kingRank * 8 + file);
            var squareMask = Bitboard.Mask(square);

            if (allPiecesWithoutPawns.Intersects(squareMask))
            {
                // If we hit an enemy rook/queen, we have a pin
                if (enemyRooksQueens.Intersects(squareMask)) return true;

                // Otherwise, this piece blocks any potential pin from this direction
                break;
            }
        }

        // Check for attacking rooks/queens to the right of the king
        for (var file = kingFile + 1; file < 8; file++)
        {
            var square = (Square)(kingRank * 8 + file);
            var squareMask = Bitboard.Mask(square);

            if (allPiecesWithoutPawns.Intersects(squareMask))
            {
                // If we hit an enemy rook/queen, we have a pin
                if (enemyRooksQueens.Intersects(squareMask)) return true;

                // Otherwise, this piece blocks any potential pin from this direction
                break;
            }
        }

        // No pin found
        return false;
    }

    private static Bitboard GetBlockingSquares(Position position, Square kingSquare, Square attackerSquare)
    {
        Bitboard blockingSquares = 0;

        // Only sliding pieces can be blocked
        var attackerPieceType = GetPieceTypeAtSquare(position, attackerSquare);
        if (!IsSlidingPiece(attackerPieceType))
        {
            return 0;
        }

        // Get squares between king and attacker
        blockingSquares = GetRayBetween(kingSquare, attackerSquare);

        return blockingSquares;
    }

    private static PieceType GetPieceTypeAtSquare(Position pos, Square square)
    {
        var mask = Bitboard.Mask(square);
        if ((pos.WhitePawns & mask) != 0) return PieceType.WhitePawn;
        if ((pos.WhiteKnights & mask) != 0) return PieceType.WhiteKnight;
        if ((pos.WhiteBishops & mask) != 0) return PieceType.WhiteBishop;
        if ((pos.WhiteRooks & mask) != 0) return PieceType.WhiteRook;
        if ((pos.WhiteQueens & mask) != 0) return PieceType.WhiteQueen;
        if ((pos.WhiteKing & mask) != 0) return PieceType.WhiteKing;
        if ((pos.BlackPawns & mask) != 0) return PieceType.BlackPawn;
        if ((pos.BlackKnights & mask) != 0) return PieceType.BlackKnight;
        if ((pos.BlackBishops & mask) != 0) return PieceType.BlackBishop;
        if ((pos.BlackRooks & mask) != 0) return PieceType.BlackRook;
        if ((pos.BlackQueens & mask) != 0) return PieceType.BlackQueen;
        if ((pos.BlackKing & mask) != 0) return PieceType.BlackKing;
        return PieceType.None;
    }

    private static bool IsSlidingPiece(PieceType pieceType)
    {
        return pieceType
            is PieceType.WhiteBishop
            or PieceType.BlackBishop
            or PieceType.WhiteRook
            or PieceType.BlackRook
            or PieceType.WhiteQueen
            or PieceType.BlackQueen;
    }

    private static Bitboard GetRayBetween(Square from, Square to)
    {
        Bitboard ray = 0;

        var fromFile = from.GetFile();
        var fromRank = from.GetRank();
        var toFile = to.GetFile();
        var toRank = to.GetRank();

        // Check if squares are aligned (same rank, file, or diagonal)
        var fileDelta = toFile - fromFile;
        var rankDelta = toRank - fromRank;

        // Squares must be aligned for a ray to exist between them
        if (fileDelta != 0 && rankDelta != 0 && Math.Abs(fileDelta) != Math.Abs(rankDelta))
        {
            return 0; // Not aligned, no ray exists
        }

        // Determine direction
        var fileStep = fileDelta == 0 ? 0 : fileDelta > 0 ? 1 : -1;
        var rankStep = rankDelta == 0 ? 0 : rankDelta > 0 ? 1 : -1;

        // Create ray (excluding the endpoint squares)
        var currentFile = fromFile + fileStep;
        var currentRank = fromRank + rankStep;

        while (currentFile != toFile || currentRank != toRank)
        {
            // Safety check to prevent infinite loops
            if (currentFile < 0 || currentFile >= 8 || currentRank < 0 || currentRank >= 8)
            {
                break;
            }

            ray |= 1UL << (currentRank * 8 + currentFile);
            currentFile += fileStep;
            currentRank += rankStep;
        }

        return ray;
    }

    private static bool IsMovingAlongPinRay(Move move, Square kingSquare)
    {
        // Get the ray direction from king to piece
        var kingFile = kingSquare.GetFile();
        var kingRank = kingSquare.GetRank();
        var pieceFile = move.From.GetFile();
        var pieceRank = move.From.GetRank();
        var targetFile = move.To.GetFile();
        var targetRank = move.To.GetRank();

        // Determine direction vector for the pin ray
        var fileDirection = pieceFile == kingFile ? 0 : pieceFile > kingFile ? 1 : -1;
        var rankDirection = pieceRank == kingRank ? 0 : pieceRank > kingRank ? 1 : -1;

        if (fileDirection == 0) // Vertical pin
        {
            return targetFile == kingFile;
        }

        if (rankDirection == 0) // Horizontal pin
        {
            return targetRank == kingRank;
        }

        // Diagonal pin
        // For a diagonal pin, check if the move maintains the same slope
        var fromKingFileDelta = pieceFile - kingFile;
        var fromKingRankDelta = pieceRank - kingRank;
        var toKingFileDelta = targetFile - kingFile;
        var toKingRankDelta = targetRank - kingRank;

        // The slopes must be equal for the move to be along the pin ray
        // Slope = rankDelta / fileDelta
        return Math.Abs(toKingFileDelta) == Math.Abs(toKingRankDelta) &&
               toKingFileDelta * fromKingFileDelta >= 0 &&  // Same file direction or through king
               toKingRankDelta * fromKingRankDelta >= 0;    // Same rank direction or through king
    }
}
