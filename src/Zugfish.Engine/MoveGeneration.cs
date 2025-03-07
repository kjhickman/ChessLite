using System.Numerics;
using System.Runtime.CompilerServices;
using Zugfish.Engine.Models;

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
            var from = currentPawns.GetFirstSquare();

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
            else if (leftCaptureTo == enPassantTarget && fromFile != 0)
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
            else if (rightCaptureTo == enPassantTarget && fromFile != 7)
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
            var from = currentKnights.GetFirstSquare();
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

        var from = king.GetFirstSquare();
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
        var kingSquare = isWhite ? position.WhiteKing.GetFirstSquare() : position.BlackKing.GetFirstSquare();

        // If moving the king, check if destination is attacked
        if (move.PieceType == (isWhite ? PieceType.WhiteKing : PieceType.BlackKing))
        {
            return !WouldSquareBeAttacked(position, move.To, !isWhite, kingSquare);
        }

        // Case 1: If the king is in check, the move must resolve the check
        if (position.IsInCheck())
        {
            var checkingPieces = FindAttackingPieces(position, kingSquare, !isWhite);
            var checkCount = checkingPieces.Count();

            // If double check, only king moves can resolve it
            if (checkCount > 1)
            {
                return false;
            }

            // Single check can be resolved by:
            // 1. Capturing the checking piece
            var checkingSquare = checkingPieces.GetFirstSquare();
            if (move.To == checkingSquare ||
                (move.SpecialMoveType == SpecialMoveType.EnPassant && move.To == checkingSquare + (isWhite ? 8 : -8)))
            {
                // Check if the piece is pinned
                return !IsPiecePinned(position, kingSquare, move);
            }

            // 2. Blocking the check (only for sliding piece checks)
            var blockingSquares = GetBlockingSquares(position, kingSquare, checkingSquare);
            return blockingSquares.Intersects(Bitboard.Mask(move.To)) && !IsPiecePinned(position, kingSquare, move);
        }

        // Case 3: Check if the piece is pinned or moving would expose king to check
        if (IsPiecePinned(position, kingSquare, move))
        {
            // Check if move stays on the pin ray
            return IsMovingAlongPinRay(move, kingSquare);
        }

        return true;
    }

    private static bool WouldSquareBeAttacked(Position position, Square targetSquare, bool byWhite, Square currentKingSquare)
    {
        var targetSquareMask = Bitboard.Mask(targetSquare);

        // Check pawn attacks
        var pawns = byWhite ? position.WhitePawns : position.BlackPawns;
        var pawnAttacks = AttackGeneration.CalculatePawnAttacks(pawns, byWhite);
        if (targetSquareMask.Intersects(pawnAttacks)) return true;

        // Check knight attacks
        var knights = byWhite ? position.WhiteKnights : position.BlackKnights;
        var knightAttacks = AttackGeneration.CalculateKnightAttacks(knights);
        if (targetSquareMask.Intersects(knightAttacks)) return true;

        // Check king attacks
        var enemyKing = byWhite ? position.WhiteKing : position.BlackKing;
        var kingAttacks = AttackGeneration.CalculateKingAttacks(enemyKing);
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

    public static Bitboard CalculateDiagonalAttacks(Bitboard allPiecesExceptKing, Bitboard diagonalRayAttackers)
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

                    if ((allPiecesExceptKing & squareMask) != 0)
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

    private static Bitboard CalculateOrthogonalAttacks(Bitboard allPiecesExceptKing, Bitboard orthogonalRayAttackers)
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

                    if ((allPiecesExceptKing & squareMask) != 0)
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

    private static Bitboard FindAttackingPieces(Position position, Square square, bool attackerIsWhite)
    {
        var pawnAttackers = FindPawnAttacks(square, position, attackerIsWhite);
        var knightAttacks = FindKnightAttacks(square, position, attackerIsWhite);
        var bishopQueenAttacks = FindDiagonalAttacks(square, position, attackerIsWhite);
        var rookQueenAttacks = FindOrthogonalAttacks(square, position, attackerIsWhite);

        // Combine all attacking pieces
        var checkingPieces = pawnAttackers | knightAttacks | bishopQueenAttacks | rookQueenAttacks;

        return checkingPieces;
    }

    private static Bitboard FindPawnAttacks(Square square, Position position, bool isWhitePawn)
    {
        Bitboard attacks = 0;
        var file = square.GetFile();
        var rank = square.GetRank();
        var rankOffset = isWhitePawn ? -1 : 1;

        if (file > 0)
        {
            var targetRank = rank + rankOffset;
            if (targetRank is >= 0 and < 8)
            {
                attacks |= 1UL << (targetRank * 8 + file - 1);
            }
        }

        if (file < 7)
        {
            var targetRank = rank + rankOffset;
            if (targetRank is >= 0 and < 8)
            {
                attacks |= 1UL << (targetRank * 8 + file + 1);
            }
        }

        var pawns = isWhitePawn ? position.WhitePawns : position.BlackPawns;
        return attacks & pawns;
    }

    private static Bitboard FindKnightAttacks(Square square, Position position, bool attackerIsWhite)
    {
        Bitboard attacks = 0;
        var file = square.GetFile();
        var rank = square.GetRank();

        Span<(int fileOffset, int rankOffset)> knightOffsets =
        [
            (1, 2), (2, 1), (2, -1), (1, -2),
            (-1, -2), (-2, -1), (-2, 1), (-1, 2)
        ];

        for (var i = 0; i < knightOffsets.Length; i++)
        {
            var (fileOffset, rankOffset) = knightOffsets[i];
            var targetFile = file + fileOffset;
            var targetRank = rank + rankOffset;

            if (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
            {
                attacks |= 1UL << (targetRank * 8 + targetFile);
            }
        }

        var knights = attackerIsWhite ? position.WhiteKnights : position.BlackKnights;
        return attacks & knights;
    }

    private static Bitboard FindDiagonalAttacks(Square square, Position position, bool attackerIsWhite)
    {
        Bitboard attacks = 0;
        var file = square.GetFile();
        var rank = square.GetRank();
        var allPieces = position.AllPieces;

        Span<(int fileDirection, int rankDirection)> diagonalDirections =
        [
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        ];

        for (var i = 0; i < diagonalDirections.Length; i++)
        {
            var (fileDirection, rankDirection) = diagonalDirections[i];
            var targetFile = file + fileDirection;
            var targetRank = rank + rankDirection;

            while (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
            {
                var targetSquare = targetRank * 8 + targetFile;
                var targetMask = 1UL << targetSquare;

                attacks |= targetMask;

                // Stop if we hit a piece
                if ((allPieces & targetMask) != 0)
                {
                    break;
                }

                targetFile += fileDirection;
                targetRank += rankDirection;
            }
        }

        var diagonalSliders = attackerIsWhite
            ? position.WhiteBishops | position.WhiteQueens
            : position.BlackBishops | position.BlackQueens;

        return attacks & diagonalSliders;
    }

    private static Bitboard FindOrthogonalAttacks(Square square, Position position, bool attackerIsWhite)
    {
        Bitboard attacks = 0;
        var file = square.GetFile();
        var rank = square.GetRank();
        var allPieces = position.AllPieces;

        Span<(int fileDirection, int rankDirection)> orthogonalDirections =
        [
            (1, 0), (-1, 0), (0, 1), (0, -1)
        ];

        for (var i = 0; i < orthogonalDirections.Length; i++)
        {
            var (fileDirection, rankDirection) = orthogonalDirections[i];
            var targetFile = file + fileDirection;
            var targetRank = rank + rankDirection;

            while (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
            {
                var targetSquare = targetRank * 8 + targetFile;
                var targetMask = 1UL << targetSquare;

                attacks |= targetMask;

                // Stop if we hit a piece
                if ((allPieces & targetMask) != 0)
                {
                    break;
                }

                targetFile += fileDirection;
                targetRank += rankDirection;
            }
        }

        var orthogonalSliders = attackerIsWhite
            ? position.WhiteRooks | position.WhiteQueens
            : position.BlackRooks | position.BlackQueens;

        return attacks & orthogonalSliders;
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

    private static bool IsPiecePinned(Position position, Square kingSquare, Move move)
    {
        var kingFile = kingSquare.GetFile();
        var kingRank = kingSquare.GetRank();
        var fromFile = move.From.GetFile();
        var fromRank = move.From.GetRank();

        var onSameFile = kingFile == fromFile;
        var onSameRank = kingRank == fromRank;
        var onSameDiagonal = Math.Abs(kingFile - fromFile) == Math.Abs(kingRank - fromRank);

        if (!onSameFile && !onSameRank && !onSameDiagonal)
        {
            return false;
        }

        if (!onSameFile && !onSameDiagonal && onSameRank)
        {
            if (move.SpecialMoveType == SpecialMoveType.EnPassant)
            {
                return IsEnPassantPinned(position, move, kingSquare);
            }
        }

        // Determine direction vector from king to piece
        var fileDirection = fromFile == kingFile ? 0 : fromFile > kingFile ? 1 : -1;
        var rankDirection = fromRank == kingRank ? 0 : fromRank > kingRank ? 1 : -1;

        // Check if there's a piece between king and our piece
        var currentFile = kingFile + fileDirection;
        var currentRank = kingRank + rankDirection;

        while ((currentFile != fromFile || currentRank != fromRank) && currentFile is >= 0 and < 8 &&
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
        currentFile = fromFile + fileDirection;
        currentRank = fromRank + rankDirection;

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

    private static bool IsEnPassantPinned(Position position, Move move, Square kingSquare)
    {
        // Only need to check for horizontal pins on the same rank as the king
        if (kingSquare.GetRank() != move.From.GetRank()) return false;

        var kingFile = kingSquare.GetFile();
        var capturedPawnFile = move.To.GetFile();
        var movingPawnFile = move.From.GetFile();
        var kingRank = kingSquare.GetRank();

        // Identify enemy rooks/queens on the same rank
        var enemyRooksQueens = position.WhiteToMove
            ? position.BlackRooks | position.BlackQueens
            : position.WhiteRooks | position.WhiteQueens;

        // Get their positions on the same rank as the king
        var kingRankMask = Bitboard.GetRankMask(kingRank);
        enemyRooksQueens &= kingRankMask;

        if (enemyRooksQueens.IsEmpty()) return false; // No enemy rooks/queens on the same rank

        // Find pieces to the right and left of the king
        var piecesToLeft = enemyRooksQueens & (Bitboard.AllOnes << (kingRank * 8)) & ~(Bitboard.AllOnes << (kingRank * 8 + kingFile));
        var piecesToRight = enemyRooksQueens & (Bitboard.AllOnes << (kingRank * 8 + kingFile + 1));

        // Check pin from left side
        if (piecesToLeft.IsNotEmpty())
        {
            // Find the rightmost set bit (closest piece to the king)
            var mask = piecesToLeft;

            // Find the index of the most significant set bit
            // This is the file of the enemy piece closest to the king from the left
            var enemySquare = Square.None;
            while (mask.IsNotEmpty())
            {
                enemySquare = mask.GetFirstSquare();
                mask &= ~Bitboard.Mask(enemySquare);
            }

            var enemyFile = enemySquare.GetFile();

            // Check if both pawns are between king and enemy
            if (enemyFile < Math.Min(movingPawnFile, capturedPawnFile) &&
                Math.Max(movingPawnFile, capturedPawnFile) < kingFile)
            {
                // Check if there are other pieces between
                var startFile = enemyFile + 1;
                var endFile = kingFile - 1;

                Bitboard betweenMask = 0;
                for (var file = startFile; file <= endFile; file++)
                {
                    if (file != movingPawnFile && file != capturedPawnFile)
                    {
                        betweenMask |= Bitboard.Mask(kingRank * 8 + file);
                    }
                }

                if ((position.AllPieces & betweenMask) == 0)
                    return true;
            }
        }

        // Check pin from right side
        if (piecesToRight != 0)
        {
            // Find the closest piece to the king
            var enemyFile = 0;
            var mask = 1UL << (kingRank * 8 + kingFile + 1);
            while ((piecesToRight & mask) == 0 && mask != 0)
            {
                enemyFile++;
                mask <<= 1;
            }

            enemyFile = kingFile + 1 + enemyFile;

            // Check if both pawns are between king and enemy
            if (kingFile < Math.Min(movingPawnFile, capturedPawnFile) &&
                Math.Max(movingPawnFile, capturedPawnFile) < enemyFile)
            {
                // Check if there are other pieces between
                var startFile = kingFile + 1;
                var endFile = enemyFile - 1;

                Bitboard betweenMask = 0;
                for (var file = startFile; file <= endFile; file++)
                {
                    if (file != movingPawnFile && file != capturedPawnFile)
                    {
                        betweenMask |= Bitboard.Mask(kingRank * 8 + file);
                    }
                }

                if ((position.AllPieces & betweenMask) == 0) return true;
            }
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
