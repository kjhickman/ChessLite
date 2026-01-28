using System.Runtime.CompilerServices;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Movement;

internal static class MoveGeneration
{
    internal static int GenerateLegalMoves(Position position, Span<Move> legalMovesBuffer)
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

                if (leftCapturedPieceType != PieceType.None)
                {
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
                if (rightCapturedPieceType != PieceType.None)
                {
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
            }
            else if (rightCaptureTo == enPassantTarget && fromFile != 7)
            {
                movesBuffer[bufferIndex++] = Move.CreateEnPassant(from, rightCaptureTo, isWhite);
            }

            // Clear the least significant bit
            currentPawns &= currentPawns - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PieceType DetermineCapturedPieceType(Position position, Bitboard toMask, bool isWhite)
    {
        if (isWhite)
        {
            if ((position.BlackPawns & toMask) != 0) return PieceType.BlackPawn;
            if ((position.BlackKnights & toMask) != 0) return PieceType.BlackKnight;
            if ((position.BlackBishops & toMask) != 0) return PieceType.BlackBishop;
            if ((position.BlackRooks & toMask) != 0) return PieceType.BlackRook;
            if ((position.BlackQueens & toMask) != 0) return PieceType.BlackQueen;
            return PieceType.None;
        }
        else
        {
            if ((position.WhitePawns & toMask) != 0) return PieceType.WhitePawn;
            if ((position.WhiteKnights & toMask) != 0) return PieceType.WhiteKnight;
            if ((position.WhiteBishops & toMask) != 0) return PieceType.WhiteBishop;
            if ((position.WhiteRooks & toMask) != 0) return PieceType.WhiteRook;
            if ((position.WhiteQueens & toMask) != 0) return PieceType.WhiteQueen;
            return PieceType.None;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateKnightMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var knights = isWhite ? position.WhiteKnights : position.BlackKnights;
        var friendlyPieces = isWhite ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var pieceType = isWhite ? PieceType.WhiteKnight : PieceType.BlackKnight;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateBishopMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var bishops = isWhite ? position.WhiteBishops : position.BlackBishops;
        var friendlyPieces = isWhite ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var pieceType = isWhite ? PieceType.WhiteBishop : PieceType.BlackBishop;

        while (bishops != 0)
        {
            var from = bishops.GetFirstSquare();
            var attacks = MagicBitboards.GetBishopAttacks(from, position.AllPieces) & ~friendlyPieces;

            // Generate captures
            var captures = attacks & enemyPieces;
            while (captures != 0)
            {
                var to = captures.GetFirstSquare();
                var capturedPieceType = DetermineCapturedPieceType(position, Bitboard.Mask(to), isWhite);
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

            bishops &= bishops - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateRookMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var rooks = isWhite ? position.WhiteRooks : position.BlackRooks;
        var friendlyPieces = isWhite ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var pieceType = isWhite ? PieceType.WhiteRook : PieceType.BlackRook;

        while (rooks != 0)
        {
            var from = rooks.GetFirstSquare();
            var attacks = MagicBitboards.GetRookAttacks(from, position.AllPieces) & ~friendlyPieces;

            // Generate captures
            var captures = attacks & enemyPieces;
            while (captures != 0)
            {
                var to = captures.GetFirstSquare();
                var capturedPieceType = DetermineCapturedPieceType(position, Bitboard.Mask(to), isWhite);
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

            rooks &= rooks - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateQueenMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var queens = isWhite ? position.WhiteQueens : position.BlackQueens;
        var friendlyPieces = isWhite ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var pieceType = isWhite ? PieceType.WhiteQueen : PieceType.BlackQueen;

        while (queens != 0)
        {
            var from = queens.GetFirstSquare();
            var attacks = MagicBitboards.GetQueenAttacks(from, position.AllPieces) & ~friendlyPieces;

            // Generate captures
            var captures = attacks & enemyPieces;
            while (captures != 0)
            {
                var to = captures.GetFirstSquare();
                var capturedPieceType = DetermineCapturedPieceType(position, Bitboard.Mask(to), isWhite);
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

            queens &= queens - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateKingMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var king = isWhite ? position.WhiteKing : position.BlackKing;
        var friendlyPieces = isWhite ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var pieceType = isWhite ? PieceType.WhiteKing : PieceType.BlackKing;

        var from = king.GetFirstSquare();

        // Use precomputed attack table directly rather than computing offsets
        var attacks = AttackTables.KingAttacks[(int)from] & ~friendlyPieces;

            // Generate captures
            var captures = attacks & enemyPieces;
            while (captures != 0)
            {
                var to = captures.GetFirstSquare();
                var capturedPieceType = DetermineCapturedPieceType(position, Bitboard.Mask(to), isWhite);
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

        // Castling moves
        if (isWhite)
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



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsSquareAttacked(Position position, Square square, bool byWhite)
    {
        var enemyAttacks = byWhite ? position.WhiteAttacks : position.BlackAttacks;
        return enemyAttacks.Intersects(square);
    }
}
