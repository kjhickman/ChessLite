using System.Runtime.CompilerServices;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Movement;

internal static class LegalityChecker
{
    internal static bool IsMoveLegal(Position position, Move move)
    {
        var isWhite = position.WhiteToMove;
        var kingSquare = isWhite ? position.WhiteKing.GetFirstSquare() : position.BlackKing.GetFirstSquare();
        var targetSquare = move.To;

        // If the king is moving, check that it doesn't move into check
        if (move.PieceType is PieceType.WhiteKing or PieceType.BlackKing)
        {
            var enemyAttacksWithoutKing = !position.WhiteToMove ? position.WhiteAttacksWithoutBlackKing : position.BlackAttacksWithoutWhiteKing;
            return !enemyAttacksWithoutKing.Intersects(targetSquare);
        }

        // If the king is in check, check if the move resolves it
        if (MoveGeneration.IsSquareAttacked(position, kingSquare, !isWhite))
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
        // If king is not on the same rank, no horizontal pin possible
        if (kingSquare.GetRank() != move.From.GetRank()) return false;

        var capturedPawnSquare = position.WhiteToMove
            ? move.To - 8
            : move.To + 8;

        // Create bitboard with both pawns removed
        var allPiecesWithoutPawns = position.AllPieces
            .ClearSquare(move.From)
            .ClearSquare(capturedPawnSquare);

        // Get enemy rooks/queens
        var enemyRooksQueens = position.WhiteToMove
            ? position.BlackRooks | position.BlackQueens
            : position.WhiteRooks | position.WhiteQueens;

        if (enemyRooksQueens == 0) return false;

        // Check for horizontal pin in ONE pass using magic bitboards
        var horizontalAttacks = MagicBitboards.GetRookAttacks(kingSquare, allPiecesWithoutPawns);
        return horizontalAttacks.Intersects(enemyRooksQueens);
    }

    private static Bitboard GetBlockingSquares(Position position, Square kingSquare, Square attackerSquare)
    {
        // Only sliding pieces can be blocked
        var attackerPieceType = GetPieceTypeAtSquare(position, attackerSquare);
        if (!IsSlidingPiece(attackerPieceType))
        {
            return 0;
        }

        // Get squares between king and attacker
        var blockingSquares = GetRayBetween(kingSquare, attackerSquare);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        // Early exit for non-aligned squares
        var fromFile = from.GetFile();
        var fromRank = from.GetRank();
        var toFile = to.GetFile();
        var toRank = to.GetRank();
        var fileDelta = toFile - fromFile;
        var rankDelta = toRank - fromRank;

        if (fileDelta != 0 && rankDelta != 0 && Math.Abs(fileDelta) != Math.Abs(rankDelta))
            return 0;

        // Use magic bitboards to get sliding piece attacks
        if (fileDelta == 0 || rankDelta == 0)
        {
            // Orthogonal ray
            var rookAttacks = MagicBitboards.GetRookAttacks(from, Bitboard.Mask(to));
            return rookAttacks & MagicBitboards.GetRookAttacks(to, Bitboard.Mask(from));
        }

        // Diagonal ray
        var bishopAttacks = MagicBitboards.GetBishopAttacks(from, Bitboard.Mask(to));
        return bishopAttacks & MagicBitboards.GetBishopAttacks(to, Bitboard.Mask(from));
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
