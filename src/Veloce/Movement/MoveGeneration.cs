using System.Runtime.CompilerServices;
using Veloce.Core;
using Veloce.State;

namespace Veloce.Movement;

public static class MoveGeneration
{
    public static int GenerateLegalMoves(Position position, Span<Move> legalMovesBuffer)
    {
        Span<Move> pseudoLegalMovesBuffer = stackalloc Move[256];
        var pseudoLegalMoveCount = GeneratePseudoLegalMoves(position, pseudoLegalMovesBuffer);

        var legalMoveCount = 0;
        for (var i = 0; i < pseudoLegalMoveCount; i++)
        {
            var move = pseudoLegalMovesBuffer[i];
            if (LegalityChecker.IsMoveLegal(position, move))
            {
                legalMovesBuffer[legalMoveCount++] = move;
            }
        }

        return legalMoveCount;
    }

    private static int GeneratePseudoLegalMoves(Position position, Span<Move> movesBuffer)
    {
        var moveCount = 0;

        GeneratePawnMoves(position, ref moveCount, movesBuffer);
        GenerateKnightMoves(position, ref moveCount, movesBuffer);
        GenerateBishopMoves(position, ref moveCount, movesBuffer);
        GenerateRookMoves(position, ref moveCount, movesBuffer);
        GenerateQueenMoves(position, ref moveCount, movesBuffer);
        GenerateKingMoves(position, ref moveCount, movesBuffer);

        return moveCount;
    }

    private static void GeneratePawnMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var pieceType = isWhite ? PieceType.WhitePawn : PieceType.BlackPawn;
        var pawns = isWhite ? position.WhitePawns : position.BlackPawns;
        var direction = isWhite ? 8 : -8;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var allPieces = position.AllPieces;
        var enPassantTarget = position.EnPassantTarget;

        var startingRank = isWhite ? Constants.SecondRank : Constants.SeventhRank;

        var currentPawns = pawns;
        while (currentPawns.IsNotEmpty())
        {
            var from = currentPawns.GetFirstSquare();

            // One square forward
            var oneStep = from + direction;
            var oneStepMask = Bitboard.Mask(oneStep);
            if (oneStep.IsValid() && oneStepMask.DoesNotIntersect(allPieces))
            {
                if ((int)oneStep is > 55 or < 8)
                {
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Queen);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Rook);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Bishop);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Knight);
                }
                else
                {
                    movesBuffer[bufferIndex++] = Move.CreateQuiet(from, oneStep, pieceType);
                }
            }

            // Two squares forward
            var twoSteps = from + direction * 2;
            var twoStepsMask = Bitboard.Mask(twoSteps) | oneStepMask; // Mask for both squares in front of the pawn
            if (twoSteps.IsValid() && twoStepsMask.DoesNotIntersect(allPieces) && Bitboard.Mask(from).Intersects(startingRank))
            {
                movesBuffer[bufferIndex++] = Move.CreateDoublePawnPush(from, twoSteps, pieceType);
            }

            // Left capture
            var leftCaptureTo = from + direction - 1;
            var leftCaptureMask = Bitboard.Mask(leftCaptureTo);
            var fromFile = from.GetFile();
            if (leftCaptureTo.IsValid() && leftCaptureMask.Intersects(enemyPieces) && fromFile != 0)
            {
                var leftCapturedPieceType = DetermineCapturedPieceType(position, leftCaptureMask, isWhite);

                if ((int)leftCaptureTo is > 55 or < 8)
                {
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Queen);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Rook);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Bishop);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Knight);
                }
                else
                {
                    movesBuffer[bufferIndex++] = Move.CreateCapture(from, leftCaptureTo, pieceType, leftCapturedPieceType);
                }
            }
            else if (leftCaptureTo == enPassantTarget && fromFile != 0)
            {
                movesBuffer[bufferIndex++] = Move.CreateEnPassant(from, leftCaptureTo, isWhite);
            }

            // Right capture
            var rightCaptureTo = from + direction + 1;
            var rightCaptureMask = Bitboard.Mask(rightCaptureTo);
            if (rightCaptureTo.IsValid() && rightCaptureMask.Intersects(enemyPieces) && fromFile != 7)
            {
                var rightCapturedPieceType = DetermineCapturedPieceType(position, rightCaptureMask, isWhite);
                if ((int)rightCaptureTo is > 55 or < 8)
                {
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Queen);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Rook);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Bishop);
                    movesBuffer[bufferIndex++] = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Knight);
                }
                else
                {
                    movesBuffer[bufferIndex++] = Move.CreateCapture(from, rightCaptureTo, pieceType, rightCapturedPieceType);
                }
            }
            else if (rightCaptureTo == enPassantTarget && fromFile != 7)
            {
                movesBuffer[bufferIndex++] = Move.CreateEnPassant(from, rightCaptureTo, isWhite);
            }

            // Clear the least significant bit
            currentPawns &= currentPawns - 1;
        }
    }

    private static PieceType DetermineCapturedPieceType(Position position, Bitboard toMask, bool isWhite)
    {
        if (isWhite)
        {
            if ((position.BlackPawns & toMask) != 0)
            {
                return PieceType.BlackPawn;
            }
            if ((position.BlackKnights & toMask) != 0)
            {
                return PieceType.BlackKnight;
            }
            if ((position.BlackBishops & toMask) != 0)
            {
                return PieceType.BlackBishop;
            }
            if ((position.BlackRooks & toMask) != 0)
            {
                return PieceType.BlackRook;
            }
            if ((position.BlackQueens & toMask) != 0)
            {
                return PieceType.BlackQueen;
            }
            if ((position.BlackKing & toMask) != 0)
            {
                return PieceType.BlackKing;
            }
        }
        else
        {
            if ((position.WhitePawns & toMask) != 0)
            {
                return PieceType.WhitePawn;
            }
            if ((position.WhiteKnights & toMask) != 0)
            {
                return PieceType.WhiteKnight;
            }
            if ((position.WhiteBishops & toMask) != 0)
            {
                return PieceType.WhiteBishop;
            }
            if ((position.WhiteRooks & toMask) != 0)
            {
                return PieceType.WhiteRook;
            }
            if ((position.WhiteQueens & toMask) != 0)
            {
                return PieceType.WhiteQueen;
            }
            if ((position.WhiteKing & toMask) != 0)
            {
                return PieceType.WhiteKing;
            }
        }

        throw new InvalidOperationException("No piece found at the target square.");
    }

    private static void GenerateKnightMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var knights = position.WhiteToMove ? position.WhiteKnights : position.BlackKnights;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;
        var pieceType = position.WhiteToMove ? PieceType.WhiteKnight : PieceType.BlackKnight;

        while (knights != 0)
        {
            var from = knights.GetFirstSquare();
            // Use precomputed attack table directly rather than computing offsets
            var attacks = AttackTables.KnightAttacks[(int)from] & ~friendlyPieces;

            // Generate captures
            var captures = attacks & enemyPieces;
            while (captures != 0)
            {
                var to = captures.GetFirstSquare();
                var capturedPieceType = DetermineCapturedPieceType(position, Bitboard.Mask(to), position.WhiteToMove);
                movesBuffer[bufferIndex++] = Move.CreateCapture(from, to, pieceType, capturedPieceType);
                captures &= captures - 1;
            }

            // Generate quiet moves
            var quiets = attacks & ~enemyPieces;
            while (quiets != 0)
            {
                var to = quiets.GetFirstSquare();
                movesBuffer[bufferIndex++] = Move.CreateQuiet(from, to, pieceType);
                quiets &= quiets - 1;
            }

            knights &= knights - 1;
        }
    }

    private static void GenerateBishopMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<(int fileDirection, int rankDirection)> bishopDirections = [(1, 1), (-1, 1), (1, -1), (-1, -1)];
        var bishops = position.WhiteToMove ? position.WhiteBishops : position.BlackBishops;
        var pieceType = position.WhiteToMove ? PieceType.WhiteBishop : PieceType.BlackBishop;
        GenerateSlidingMoves(bishops, pieceType, position, ref bufferIndex, movesBuffer, bishopDirections);
    }

    private static void GenerateRookMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<(int fileDirection, int rankDirection)> rookDirections = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        var rooks = position.WhiteToMove ? position.WhiteRooks : position.BlackRooks;
        var pieceType = position.WhiteToMove ? PieceType.WhiteRook : PieceType.BlackRook;
        GenerateSlidingMoves(rooks, pieceType, position, ref bufferIndex, movesBuffer, rookDirections);
    }

    private static void GenerateQueenMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<(int fileDirection, int rankDirection)> queenDirections =
        [
            (1, 1), (-1, 1), (1, -1), (-1, -1),
            (1, 0), (-1, 0), (0, 1), (0, -1),
        ];
        var queens = position.WhiteToMove ? position.WhiteQueens : position.BlackQueens;
        var pieceType = position.WhiteToMove ? PieceType.WhiteQueen : PieceType.BlackQueen;
        GenerateSlidingMoves(queens, pieceType, position, ref bufferIndex, movesBuffer, queenDirections);
    }

    private static void GenerateKingMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var king = position.WhiteToMove ? position.WhiteKing : position.BlackKing;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;
        var pieceType = position.WhiteToMove ? PieceType.WhiteKing : PieceType.BlackKing;

        var from = king.GetFirstSquare();
        var fromFile = from.GetFile();
        var fromRank = from.GetRank();

        Span<(int fileDirection, int rankDirection)> kingDirections = stackalloc (int, int)[]
        {
            (1, 1), (-1, 1), (1, -1), (-1, -1),
            (1, 0), (-1, 0), (0, 1), (0, -1),
        };

        for (var i = 0; i < kingDirections.Length; i++)
        {
            var (fileDirection, rankDirection) = kingDirections[i];
            var newFile = fromFile + fileDirection;
            var newRank = fromRank + rankDirection;

            if (newFile < 0 || newFile >= 8 || newRank < 0 || newRank >= 8)
            {
                // Out of bounds
                continue;
            }

            var to = (Square)(newRank * 8 + newFile);
            var toMask = Bitboard.Mask(to);

            if (toMask.Intersects(friendlyPieces))
            {
                continue;
            }

            if (toMask.Intersects(enemyPieces))
            {
                var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                movesBuffer[bufferIndex++] = Move.CreateCapture(from, to, pieceType, capturedPieceType);
            }
            else
            {
                movesBuffer[bufferIndex++] = Move.CreateQuiet(from, to, pieceType);
            }
        }

        // Castling moves
        if (position.WhiteToMove)
        {
            if (from != Square.e1) return;

            if (position.CastlingRights.Contains(CastlingRights.WhiteKingside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.WhiteShortCastleEmptySquares)
                    && !IsSquareAttacked(position, Square.e1, false)
                    && !IsSquareAttacked(position, Square.f1, false)
                    && !IsSquareAttacked(position, Square.g1, false))
                {
                    movesBuffer[bufferIndex++] = Move.CreateShortCastle(position.WhiteToMove);
                }
            }

            if (position.CastlingRights.Contains(CastlingRights.WhiteQueenside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.WhiteLongCastleEmptySquares)
                    && !IsSquareAttacked(position, Square.e1, false)
                    && !IsSquareAttacked(position, Square.d1, false)
                    && !IsSquareAttacked(position, Square.c1, false))
                {
                    movesBuffer[bufferIndex++] = Move.CreateLongCastle(position.WhiteToMove);
                }
            }
        }
        else
        {
            if (from != Square.e8) return;

            if (position.CastlingRights.Contains(CastlingRights.BlackKingside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.BlackShortCastleEmptySquares)
                    && !IsSquareAttacked(position, Square.e8, true)
                    && !IsSquareAttacked(position, Square.f8, true)
                    && !IsSquareAttacked(position, Square.g8, true))
                {
                    movesBuffer[bufferIndex++] = Move.CreateShortCastle(position.WhiteToMove);
                }
            }

            if (position.CastlingRights.Contains(CastlingRights.BlackQueenside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.BlackLongCastleEmptySquares)
                    && !IsSquareAttacked(position, Square.e8, true)
                    && !IsSquareAttacked(position, Square.d8, true)
                    && !IsSquareAttacked(position, Square.c8, true))
                {
                    movesBuffer[bufferIndex++] = Move.CreateLongCastle(position.WhiteToMove);
                }
            }
        }
    }

    private static void GenerateSlidingMoves(
        Bitboard pieces,
        PieceType pieceType,
        Position position,
        ref int bufferIndex,
        Span<Move> movesBuffer,
        ReadOnlySpan<(int fileDirection, int rankDirection)> directions)
    {
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

        while (pieces.IsNotEmpty())
        {
            var from = pieces.GetFirstSquare();
            var fromFile = from.GetFile();
            var fromRank = from.GetRank();

            for (var i = 0; i < directions.Length; i++)
            {
                var (fileDirection, rankDirection) = directions[i];
                var currentFile = fromFile;
                var currentRank = fromRank;
                while (true)
                {
                    currentFile += fileDirection;
                    currentRank += rankDirection;
                    if (currentFile is < 0 or >= 8 || currentRank is < 0 or >= 8)
                        break;
                    var to = (Square)(currentRank * 8 + currentFile);
                    var toMask = Bitboard.Mask(to);
                    if (toMask.Intersects(friendlyPieces))
                    {
                        break;
                    }

                    if (toMask.Intersects(enemyPieces))
                    {
                        var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                        movesBuffer[bufferIndex++] = Move.CreateCapture(from, to, pieceType, capturedPieceType);
                        break;
                    }

                    movesBuffer[bufferIndex++] = Move.CreateQuiet(from, to, pieceType);
                }
            }

            pieces &= pieces - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSquareAttacked(Position position, Square square, bool byWhite)
    {
        var enemyAttacks = byWhite ? position.WhiteAttacks : position.BlackAttacks;
        return enemyAttacks.Intersects(square);
    }
}
