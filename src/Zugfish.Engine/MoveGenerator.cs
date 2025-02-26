using System.Runtime.CompilerServices;
using Zugfish.Engine.Models;
using static System.Numerics.BitOperations;

namespace Zugfish.Engine;

public static class MoveGenerator
{
    // TODO: convert all these ints to Square

    public static int GenerateLegalMoves(Position position, Span<Move> movesBuffer)
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

        const ulong secondRank = 0xFF00;
        const ulong seventhRank = 0xFF000000000000;
        var startingRank = isWhite ? secondRank : seventhRank;

        var currentPawns = pawns;
        while (currentPawns != 0)
        {
            // Get the position of the least significant bit
            var from = TrailingZeroCount(currentPawns);

            // One square forward
            var oneStep = from + direction;
            Bitboard oneStepMask = 1UL << oneStep;
            if (oneStep is >= 0 and < 64 && (allPieces & oneStepMask) == 0)
            {
                if (oneStep is > 55 or < 8)
                {
                    var queenPromotion = Move.CreatePromotion((Square)from, (Square)oneStep, pieceType, PromotedPieceType.Queen);
                    var rookPromotion = Move.CreatePromotion((Square)from, (Square)oneStep, pieceType, PromotedPieceType.Rook);
                    var bishopPromotion = Move.CreatePromotion((Square)from, (Square)oneStep, pieceType, PromotedPieceType.Bishop);
                    var knightPromotion = Move.CreatePromotion((Square)from, (Square)oneStep, pieceType, PromotedPieceType.Knight);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, queenPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, rookPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, bishopPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, knightPromotion);
                }
                else
                {
                    var move = Move.CreateQuiet((Square)from, (Square)oneStep, pieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
                }

            }

            // Two squares forward
            var twoSteps = from + direction * 2;
            var twoStepsMask = (1UL << twoSteps) | oneStepMask; // Mask for both squares in front of the pawn
            if (twoSteps is >= 0 and < 64 && (allPieces & twoStepsMask) == 0 && (startingRank & (1UL << from)) != 0)
            {
                var move = Move.CreateDoublePawnPush((Square)from, (Square)twoSteps, pieceType);
                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
            }

            // Left capture
            var leftCaptureTo = from + direction - 1;
            var leftCaptureMask = 1UL << leftCaptureTo;
            var fromFile = from % 8;
            if (leftCaptureTo is >= 0 and < 64 && (enemyPieces & leftCaptureMask) != 0 && fromFile != 0)
            {
                var leftCapturedPieceType = DetermineCapturedPieceType(position, leftCaptureMask, isWhite);

                if (leftCaptureTo is > 55 or < 8)
                {
                    var queenPromotion = Move.CreatePromotion((Square)from, (Square)leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Queen);
                    var rookPromotion = Move.CreatePromotion((Square)from, (Square)leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Rook);
                    var bishopPromotion = Move.CreatePromotion((Square)from, (Square)leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Bishop);
                    var knightPromotion = Move.CreatePromotion((Square)from, (Square)leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Knight);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, queenPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, rookPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, bishopPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, knightPromotion);
                }
                else
                {
                    var move = Move.CreateCapture((Square)from, (Square)leftCaptureTo, pieceType, leftCapturedPieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
                }
            }
            else if (leftCaptureTo == (int)enPassantTarget)
            {
                var move = Move.CreateEnPassant((Square)from, (Square)leftCaptureTo, isWhite);
                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
            }

            // Right capture
            var rightCaptureTo = from + direction + 1;
            var rightCaptureMask = 1UL << rightCaptureTo;
            if (rightCaptureTo is >= 0 and < 64 && (enemyPieces & rightCaptureMask) != 0 && fromFile != 7)
            {
                var rightCapturedPieceType = DetermineCapturedPieceType(position, rightCaptureMask, isWhite);
                if (rightCaptureTo is > 55 or < 8)
                {
                    var queenPromotion = Move.CreatePromotion((Square)from, (Square)rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Queen);
                    var rookPromotion = Move.CreatePromotion((Square)from, (Square)rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Rook);
                    var bishopPromotion = Move.CreatePromotion((Square)from, (Square)rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Bishop);
                    var knightPromotion = Move.CreatePromotion((Square)from, (Square)rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Knight);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, queenPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, rookPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, bishopPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, knightPromotion);
                }
                else
                {
                    var move = Move.CreateCapture((Square)from, (Square)rightCaptureTo, pieceType, rightCapturedPieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
                }
            }
            else if (rightCaptureTo == (int)enPassantTarget)
            {
                var move = Move.CreateEnPassant((Square)from, (Square)rightCaptureTo, isWhite);
                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
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
        Span<int> knightOffsets = [17, 15, 10, 6, -6, -10, -15, -17];
        var knights = position.WhiteToMove ? position.WhiteKnights : position.BlackKnights;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;
        var pieceType = position.WhiteToMove ? PieceType.WhiteKnight : PieceType.BlackKnight;

        var currentKnights = knights;
        while (currentKnights != 0)
        {
            var from = TrailingZeroCount(currentKnights);
            var fromFile = from % 8;
            var fromRank = from / 8;

            for (var i = 0;  i < knightOffsets.Length; i++)
            {
                var to = from + knightOffsets[i];
                if (to is < 0 or >= 64)
                {
                    // Out of bounds
                    continue;
                }

                var toFile = to % 8;
                var toRank = to / 8;

                if (Math.Abs(toFile - fromFile) > 2 || Math.Abs(toRank - fromRank) > 2)
                {
                    // Invalid move (wraps around the board)
                    continue;
                }

                var toMask = Bitboard.Mask(to);
                if ((friendlyPieces & toMask) != 0)
                {
                    // Friendly piece at the target square
                    continue;
                }

                Move move;
                if ((enemyPieces & toMask) != 0)
                {
                    var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                    move = Move.CreateCapture((Square)from, (Square)to, pieceType, capturedPieceType);
                }
                else
                {
                    move = Move.CreateQuiet((Square)from, (Square)to, pieceType);
                }

                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
            }

            currentKnights &= currentKnights - 1;
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
            (1, 0), (-1, 0), (0, 1), (0, -1)
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

        var from = TrailingZeroCount(king);
        var fromFile = from % 8;
        var fromRank = from / 8;

        for (var fileDirection = -1; fileDirection <= 1; fileDirection++)
        {
            for (var rankDirection = -1; rankDirection <= 1; rankDirection++)
            {
                if (fileDirection == 0 && rankDirection == 0)
                {
                    // Skip the king's current position
                    continue;
                }

                var newFile = fromFile + fileDirection;
                var newRank = fromRank + rankDirection;

                if (newFile < 0 || newFile >= 8 || newRank < 0 || newRank >= 8)
                {
                    // Out of bounds
                    continue;
                }

                var to = newFile + newRank * 8;
                var toMask = Bitboard.Mask(to);

                if ((friendlyPieces & toMask) != 0)
                {
                    // Friendly piece at the target square
                    continue;
                }

                Move move;
                if ((enemyPieces & toMask) != 0)
                {
                    var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                    move = Move.CreateCapture((Square)from, (Square)to, pieceType, capturedPieceType);
                }
                else
                {
                    move = Move.CreateQuiet((Square)from, (Square)to, pieceType);
                }

                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
            }
        }

        // Castling moves
        if (position.WhiteToMove)
        {
            if (from != 4) return;

            // --- White kingside castling (e1 -> g1) ---
            if ((position.CastlingRights & CastlingRights.WhiteKingside) != 0)
            {
                // Squares f1 (5) and g1 (6) must be empty.
                const ulong kingsideEmptySquares = (1UL << 5) | (1UL << 6);
                if ((position.AllPieces & kingsideEmptySquares) == 0 &&
                    // The king's start square and the squares it passes through must not be attacked.
                    !position.IsSquareAttacked(4, false) &&
                    !position.IsSquareAttacked(5, false) &&
                    !position.IsSquareAttacked(6, false))
                {
                    movesBuffer[bufferIndex++] = Move.CreateShortCastle(position.WhiteToMove);
                }
            }

            // --- White queenside castling (e1 -> c1) ---
            if ((position.CastlingRights & CastlingRights.WhiteQueenside) != 0)
            {
                // Squares between the king and rook must be empty: b1 (1), c1 (2), and d1 (3).
                const ulong queensideEmptySquares = (1UL << 1) | (1UL << 2) | (1UL << 3);
                if ((position.AllPieces & queensideEmptySquares) == 0 &&
                    !position.IsSquareAttacked(4, false) &&
                    !position.IsSquareAttacked(3, false) && // d1 must not be attacked
                    !position.IsSquareAttacked(2, false))   // c1 must not be attacked
                {
                    movesBuffer[bufferIndex++] = Move.CreateLongCastle(position.WhiteToMove);
                }
            }
        }
        else
        {
            if (from != 60) return;

            // --- Black kingside castling (e8 -> g8) ---
            if ((position.CastlingRights & CastlingRights.BlackKingside) != 0)
            {
                // Squares f8 (61) and g8 (62) must be empty.
                var kingsideEmptySquares = (1UL << 61) | (1UL << 62);
                if ((position.AllPieces & kingsideEmptySquares) == 0 &&
                    !position.IsSquareAttacked(60, true) &&
                    !position.IsSquareAttacked(61, true) &&
                    !position.IsSquareAttacked(62, true))
                {
                    movesBuffer[bufferIndex++] = Move.CreateShortCastle(position.WhiteToMove);
                }
            }

            // --- Black queenside castling (e8 -> c8) ---
            if ((position.CastlingRights & CastlingRights.BlackQueenside) != 0)
            {
                // Squares between king and rook: b8 (57), c8 (58), and d8 (59) must be empty.
                var queensideEmptySquares = (1UL << 57) | (1UL << 58) | (1UL << 59);
                if ((position.AllPieces & queensideEmptySquares) == 0 &&
                    !position.IsSquareAttacked(60, true) &&
                    !position.IsSquareAttacked(59, true) &&
                    !position.IsSquareAttacked(58, true))
                {
                    movesBuffer[bufferIndex++] = Move.CreateLongCastle(position.WhiteToMove);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        while (pieces != 0)
        {
            var from = TrailingZeroCount(pieces);
            var fromFile = from % 8;
            var fromRank = from / 8;

            foreach (var (fileDirection, rankDirection) in directions)
            {
                int currentFile = fromFile;
                int currentRank = fromRank;
                while (true)
                {
                    currentFile += fileDirection;
                    currentRank += rankDirection;
                    if (currentFile is < 0 or >= 8 || currentRank is < 0 or >= 8)
                        break;
                    var to = currentRank * 8 + currentFile;
                    var toMask = Bitboard.Mask(to);
                    if ((friendlyPieces & toMask) != 0)
                        break;

                    if ((enemyPieces & toMask) != 0)
                    {
                        var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                        var captureMove = Move.CreateCapture((Square)from, (Square)to, pieceType, capturedPieceType);
                        AddMoveIfLegal(position, ref bufferIndex, movesBuffer, captureMove);
                        break;
                    }

                    var quietMove = Move.CreateQuiet((Square)from, (Square)to, pieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, quietMove);
                }
            }
            pieces &= pieces - 1; // Clear the lowest set bit
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddMoveIfLegal(Position position, ref int bufferIndex, Span<Move> movesBuffer, Move move)
    {
        if (IsMoveLegal(position, move))
        {
            movesBuffer[bufferIndex++] = move;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMoveLegal(Position position, Move move)
    {
        position.MakeMove(move);
        var kingSquare = TrailingZeroCount(position.WhiteToMove ? position.BlackKing : position.WhiteKing);
        var isLegal = !position.IsSquareAttacked(kingSquare, position.WhiteToMove);
        position.UndoMove();
        return isLegal;
    }
}
