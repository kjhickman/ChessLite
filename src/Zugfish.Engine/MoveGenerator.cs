using System.Runtime.CompilerServices;
using Zugfish.Engine.Models;
// using static System.Numerics.BitOperations;

namespace Zugfish.Engine;

public static class MoveGeneration
{
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

        var startingRank = isWhite ? Constants.SecondRank : Constants.SeventhRank;

        var currentPawns = pawns;
        while (currentPawns.IsNotEmpty())
        {
            var from = currentPawns.LsbSquare();

            // One square forward
            var oneStep = from + direction;
            var oneStepMask = Bitboard.Mask(oneStep);
            if (oneStep.IsValid() && oneStepMask.DoesNotIntersect(allPieces))
            {
                if ((int)oneStep is > 55 or < 8)
                {
                    var queenPromotion = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Queen);
                    var rookPromotion = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Rook);
                    var bishopPromotion = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Bishop);
                    var knightPromotion = Move.CreatePromotion(from, oneStep, pieceType, PromotedPieceType.Knight);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, queenPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, rookPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, bishopPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, knightPromotion);
                }
                else
                {
                    var move = Move.CreateQuiet(from, oneStep, pieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
                }
            }

            // Two squares forward
            var twoSteps = from + direction * 2;
            var twoStepsMask = Bitboard.Mask(twoSteps) | oneStepMask; // Mask for both squares in front of the pawn
            if (twoSteps.IsValid() && twoStepsMask.DoesNotIntersect(allPieces) && Bitboard.Mask(from).Intersects(startingRank))
            {
                var move = Move.CreateDoublePawnPush(from, twoSteps, pieceType);
                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
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
                    var queenPromotion = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Queen);
                    var rookPromotion = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Rook);
                    var bishopPromotion = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Bishop);
                    var knightPromotion = Move.CreatePromotion(from, leftCaptureTo, pieceType, leftCapturedPieceType, PromotedPieceType.Knight);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, queenPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, rookPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, bishopPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, knightPromotion);
                }
                else
                {
                    var move = Move.CreateCapture(from, leftCaptureTo, pieceType, leftCapturedPieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
                }
            }
            else if (leftCaptureTo >= 0 && leftCaptureTo == enPassantTarget)
            {
                var move = Move.CreateEnPassant(from, leftCaptureTo, isWhite);
                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
            }

            // Right capture
            var rightCaptureTo = from + direction + 1;
            var rightCaptureMask = Bitboard.Mask(rightCaptureTo);
            if (rightCaptureTo.IsValid() && rightCaptureMask.Intersects(enemyPieces) && fromFile != 7)
            {
                var rightCapturedPieceType = DetermineCapturedPieceType(position, rightCaptureMask, isWhite);
                if ((int)rightCaptureTo is > 55 or < 8)
                {
                    var queenPromotion = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Queen);
                    var rookPromotion = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Rook);
                    var bishopPromotion = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Bishop);
                    var knightPromotion = Move.CreatePromotion(from, rightCaptureTo, pieceType, rightCapturedPieceType, PromotedPieceType.Knight);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, queenPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, rookPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, bishopPromotion);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, knightPromotion);
                }
                else
                {
                    var move = Move.CreateCapture(from, rightCaptureTo, pieceType, rightCapturedPieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
                }
            }
            else if (rightCaptureTo == enPassantTarget)
            {
                var move = Move.CreateEnPassant(from, rightCaptureTo, isWhite);
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
        while (currentKnights.IsNotEmpty())
        {
            var from = currentKnights.LsbSquare();
            var fromFile = from.GetFile();
            var fromRank = from.GetRank();

            for (var i = 0;  i < knightOffsets.Length; i++)
            {
                var to = from + knightOffsets[i];
                if (!to.IsValid())
                {
                    // Out of bounds
                    continue;
                }

                var toFile = to.GetFile();
                var toRank = to.GetRank();

                if (Math.Abs(toFile - fromFile) > 2 || Math.Abs(toRank - fromRank) > 2)
                {
                    // Invalid move (wraps around the board)
                    continue;
                }

                var toMask = Bitboard.Mask(to);
                if (toMask.Intersects(friendlyPieces))
                {
                    continue;
                }

                Move move;
                if (toMask.Intersects(enemyPieces))
                {
                    var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                    move = Move.CreateCapture(from, to, pieceType, capturedPieceType);
                }
                else
                {
                    move = Move.CreateQuiet(from, to, pieceType);
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

        var from = king.LsbSquare();
        var fromFile = from.GetFile();
        var fromRank = from.GetRank();

        // TODO: refactor to use single loop like queen directions?
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

                var to = (Square)(newFile + newRank * 8);
                var toMask = Bitboard.Mask(to);

                if (toMask.Intersects(friendlyPieces))
                {
                    continue;
                }

                Move move;
                if (toMask.Intersects(enemyPieces))
                {
                    var capturedPieceType = DetermineCapturedPieceType(position, toMask, position.WhiteToMove);
                    move = Move.CreateCapture(from, to, pieceType, capturedPieceType);
                }
                else
                {
                    move = Move.CreateQuiet(from, to, pieceType);
                }

                AddMoveIfLegal(position, ref bufferIndex, movesBuffer, move);
            }
        }

        // Castling moves
        if (position.WhiteToMove)
        {
            if (from != Square.e1) return;

            if (position.CastlingRights.Contains(CastlingRights.WhiteKingside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.WhiteShortCastleEmptySquares)
                    && !position.IsSquareAttacked(Square.e1, false)
                    && !position.IsSquareAttacked(Square.f1, false)
                    && !position.IsSquareAttacked(Square.g1, false))
                {
                    movesBuffer[bufferIndex++] = Move.CreateShortCastle(position.WhiteToMove);
                }
            }

            if (position.CastlingRights.Contains(CastlingRights.WhiteQueenside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.WhiteLongCastleEmptySquares)
                    && !position.IsSquareAttacked(Square.e1, false)
                    && !position.IsSquareAttacked(Square.d1, false)
                    && !position.IsSquareAttacked(Square.c1, false))
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
                    && !position.IsSquareAttacked(Square.e8, true)
                    && !position.IsSquareAttacked(Square.f8, true)
                    && !position.IsSquareAttacked(Square.g8, true))
                {
                    movesBuffer[bufferIndex++] = Move.CreateShortCastle(position.WhiteToMove);
                }
            }

            if (position.CastlingRights.Contains(CastlingRights.BlackQueenside))
            {
                if (position.AllPieces.DoesNotIntersect(Constants.BlackLongCastleEmptySquares)
                    && !position.IsSquareAttacked(Square.e8, true)
                    && !position.IsSquareAttacked(Square.d8, true)
                    && !position.IsSquareAttacked(Square.c8, true))
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

        while (pieces.IsNotEmpty())
        {
            var from = pieces.LsbSquare();
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
                        var captureMove = Move.CreateCapture(from, to, pieceType, capturedPieceType);
                        AddMoveIfLegal(position, ref bufferIndex, movesBuffer, captureMove);
                        break;
                    }

                    var quietMove = Move.CreateQuiet(from, to, pieceType);
                    AddMoveIfLegal(position, ref bufferIndex, movesBuffer, quietMove);
                }
            }

            pieces &= pieces - 1;
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

    private static bool IsMoveLegal(Position position, Move move)
    {
        var isWhite = position.WhiteToMove;
        var kingSquare = isWhite ? position.WhiteKing.LsbSquare() : position.BlackKing.LsbSquare();

        // Case 1: If the king is in check, the move must resolve the check
        if (position.IsInCheck())
        {
            // TODO: optimize this to not make/unmake move
            position.MakeMove(move);
            var stillInCheck = position.IsInCheck(!position.WhiteToMove); // Check if still in check
            position.UndoMove();
            return !stillInCheck;
        }

        // Case 2: If moving the king, check if destination is attacked
        if (move.PieceType == (isWhite ? PieceType.WhiteKing : PieceType.BlackKing))
        {
            return !position.IsSquareAttacked(move.To, !isWhite);
        }

        // Case 3: Check if the piece is pinned or moving would expose king to check
        if (IsPiecePinned(position, move.From, kingSquare))
        {
            // Check if move stays on the pin ray
            return IsMovingAlongPinRay(move, kingSquare);
        }

        return true;
    }

    private static bool IsPiecePinned(Position position, Square pieceSquare, Square kingSquare)
    {
        var kingFile = kingSquare.GetFile();
        var kingRank = kingSquare.GetRank();
        var pieceFile = pieceSquare.GetFile();
        var pieceRank = pieceSquare.GetRank();

        var onSameFile = kingFile == pieceFile;
        var onSameRank = kingRank == pieceRank;
        var onSameDiagonal = Math.Abs(kingFile - pieceFile) == Math.Abs(kingRank - pieceRank);

        if (!onSameFile && !onSameRank && !onSameDiagonal)
        {
            return false;
        }

        // Determine direction vector from king to piece
        var fileDirection = pieceFile == kingFile ? 0 : pieceFile > kingFile ? 1 : -1;
        var rankDirection = pieceRank == kingRank ? 0 : pieceRank > kingRank ? 1 : -1;

        // Check if there's a piece between king and our piece
        var currentFile = kingFile + fileDirection;
        var currentRank = kingRank + rankDirection;

        while ((currentFile != pieceFile || currentRank != pieceRank) && currentFile is >= 0 and < 8 &&
               currentRank is >= 0 and < 8)
        {
            var currentSquare = currentRank * 8 + currentFile;
            if (position.AllPieces.Intersects(Bitboard.Mask(currentSquare)))
            {
                return false;
            }

            currentFile += fileDirection;
            currentRank += rankDirection;
        }

        // Continue in the same direction past our piece to find potential pinning pieces
        currentFile = pieceFile + fileDirection;
        currentRank = pieceRank + rankDirection;

        var isDiagonal = fileDirection != 0 && rankDirection != 0;
        var isOrthogonal = fileDirection == 0 || rankDirection == 0;

        // TODO: make this readable
        var enemySliders = position.WhiteToMove
            ?
            isDiagonal ? position.BlackBishops | position.BlackQueens :
            isOrthogonal ? position.BlackRooks | position.BlackQueens : 0UL
            : isDiagonal
                ? position.WhiteBishops | position.WhiteQueens
                : isOrthogonal
                    ? position.WhiteRooks | position.WhiteQueens
                    : 0UL;

        while (currentFile is >= 0 and < 8 && currentRank is >= 0 and < 8)
        {
            var currentSquare = currentRank * 8 + currentFile;
            var squareMask = Bitboard.Mask(currentSquare);

            if (squareMask.Intersects(position.AllPieces))
            {
                return enemySliders.Intersects(squareMask);
            }

            currentFile += fileDirection;
            currentRank += rankDirection;
        }

        return false;
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

        // If destination is in the opposite direction of the pin, it's illegal
        var toKingFileDelta = targetFile - kingFile;
        var toKingRankDelta = targetRank - kingRank;

        // Check if destination is on the same ray
        if (fileDirection == 0) // Vertical pin
        {
            return targetFile == kingFile;
        }

        if (rankDirection == 0) // Horizontal pin
        {
            return targetRank == kingRank;
        }

        // Diagonal pin
        // For a diagonal, the absolute deltas must be equal
        if (Math.Abs(toKingFileDelta) == Math.Abs(toKingRankDelta) &&
            (Math.Sign(toKingFileDelta) == Math.Sign(fileDirection) ||
             Math.Sign(toKingRankDelta) == Math.Sign(rankDirection)))
        {
            return true;
        }

        return false;
    }
}
