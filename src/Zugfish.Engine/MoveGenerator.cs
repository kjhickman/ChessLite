using Zugfish.Engine.Models;
using static System.Numerics.BitOperations;

namespace Zugfish.Engine;

public class MoveGenerator
{
    public int GenerateLegalMoves(Position position, Span<Move> movesBuffer)
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

    private void GeneratePawnMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var isWhite = position.WhiteToMove;
        var pawns = isWhite ? position.WhitePawns : position.BlackPawns;
        var direction = isWhite ? 8 : -8;
        var enemyPieces = isWhite ? position.BlackPieces : position.WhitePieces;
        var allPieces = position.AllPieces;

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
                Move move;
                if (oneStep is > 55 or < 8)
                {
                    move = new Move(from, oneStep, MoveType.PromoteToQueen); // TODO: Add other promotions
                }
                else
                {
                    move = new Move(from, oneStep, MoveType.Quiet);
                }

                if (IsMoveLegal(position, move))
                {
                    movesBuffer[bufferIndex++] = move;
                }
            }

            // Two squares forward
            var twoSteps = from + direction * 2;
            var twoStepsMask = (1UL << twoSteps) | oneStepMask; // Mask for both squares in front of the pawn
            if (twoSteps is >= 0 and < 64 && (allPieces & twoStepsMask) == 0 && (startingRank & (1UL << from)) != 0)
            {
                var move = new Move(from, twoSteps, MoveType.DoublePawnPush);
                if (IsMoveLegal(position, move))
                {
                    movesBuffer[bufferIndex++] = move;
                }
            }

            // Left capture
            var leftCaptureTo = from + direction - 1;
            var leftCaptureMask = 1UL << leftCaptureTo;
            var fromFile = from % 8;
            if (leftCaptureTo is >= 0 and < 64 && (enemyPieces & leftCaptureMask) != 0 && fromFile != 0)
            {
                Move move;
                if (leftCaptureTo is > 55 or < 8)
                {
                    move = new Move(from, leftCaptureTo, MoveType.PromoteToQueen); // TODO: Add other promotions
                }
                else
                {
                    move = new Move(from, leftCaptureTo, MoveType.Capture);
                }

                if (IsMoveLegal(position, move))
                {
                    movesBuffer[bufferIndex++] = move;
                }
            }

            // Right capture
            var rightCaptureTo = from + direction + 1;
            var rightCaptureMask = 1UL << rightCaptureTo;
            if (rightCaptureTo is >= 0 and < 64 && (enemyPieces & rightCaptureMask) != 0 && fromFile != 7)
            {
                Move move;
                if (rightCaptureTo is > 55 or < 8)
                {
                    move = new Move(from, rightCaptureTo, MoveType.PromoteToQueen); // TODO: Add other promotions
                }
                else
                {
                    move = new Move(from, rightCaptureTo, MoveType.Capture);
                }

                if (IsMoveLegal(position, move))
                {
                    movesBuffer[bufferIndex++] = move;
                }
            }

            // Clear the least significant bit
            currentPawns &= currentPawns - 1;
        }
    }

    private void GenerateKnightMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<int> knightOffsets = [17, 15, 10, 6, -6, -10, -15, -17];
        var knights = position.WhiteToMove ? position.WhiteKnights : position.BlackKnights;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

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
                    move = new Move(from, to, MoveType.Capture);
                }
                else
                {
                    move = new Move(from, to, MoveType.Quiet);
                }

                if (IsMoveLegal(position, move))
                {
                    movesBuffer[bufferIndex++] = move;
                }
            }

            currentKnights &= currentKnights - 1;
        }
    }

    private void GenerateBishopMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<(int fileDirection, int rankDirection)> bishopDirections =
        [
            (1, 1), (-1, 1), (1, -1), (-1, -1)
        ];
        var bishops = position.WhiteToMove ? position.WhiteBishops : position.BlackBishops;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

        var currentBishops = bishops;
        while (currentBishops != 0)
        {
            var from = TrailingZeroCount(currentBishops);
            var fromFile = from % 8;
            var fromRank = from / 8;

            for (var i = 0; i < bishopDirections.Length; i++)
            {
                var currentFile = fromFile;
                var currentRank = fromRank;

                while (true)
                {
                    currentFile += bishopDirections[i].fileDirection;
                    currentRank += bishopDirections[i].rankDirection;

                    if (currentFile is < 0 or >= 8 || currentRank is < 0 or >= 8)
                    {
                        // Out of bounds
                        break;
                    }

                    var to = currentFile + currentRank * 8;
                    var toMask = Bitboard.Mask(to);

                    if ((friendlyPieces & toMask) != 0)
                    {
                        // Friendly piece at the target square
                        break;
                    }

                    if ((enemyPieces & toMask) != 0)
                    {
                        var captureMove = new Move(from, to, MoveType.Capture);
                        if (IsMoveLegal(position, captureMove))
                        {
                            movesBuffer[bufferIndex++] = captureMove;
                        }

                        break;
                    }

                    var quietMove = new Move(from, to, MoveType.Quiet);
                    if (IsMoveLegal(position, quietMove))
                    {
                        movesBuffer[bufferIndex++] = quietMove;
                    }
                }
            }

            currentBishops &= currentBishops - 1;
        }
    }

    private void GenerateRookMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<(int fileDirection, int rankDirection)> rookDirections =
        [
            (1, 0), (-1, 0), (0, 1), (0, -1)
        ];
        var rooks = position.WhiteToMove ? position.WhiteRooks : position.BlackRooks;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

        var currentRooks = rooks;
        while (currentRooks != 0)
        {
            var from = TrailingZeroCount(currentRooks);
            var fromFile = from % 8;
            var fromRank = from / 8;

            for (var i = 0; i < rookDirections.Length; i++)
            {
                var currentFile = fromFile;
                var currentRank = fromRank;

                while (true)
                {
                    currentFile += rookDirections[i].fileDirection;
                    currentRank += rookDirections[i].rankDirection;

                    if (currentFile is < 0 or >= 8 || currentRank is < 0 or >= 8)
                    {
                        // Out of bounds
                        break;
                    }

                    var to = currentFile + currentRank * 8;
                    var toMask = Bitboard.Mask(to);

                    if ((friendlyPieces & toMask) != 0)
                    {
                        // Friendly piece at the target square
                        break;
                    }

                    if ((enemyPieces & toMask) != 0)
                    {
                        var captureMove = new Move(from, to, MoveType.Capture);
                        if (IsMoveLegal(position, captureMove))
                        {
                            movesBuffer[bufferIndex++] = captureMove;
                        }

                        break;
                    }

                    var quietMove = new Move(from, to, MoveType.Quiet);
                    if (IsMoveLegal(position, quietMove))
                    {
                        movesBuffer[bufferIndex++] = quietMove;
                    }
                }
            }


            currentRooks &= currentRooks - 1;
        }
    }

    private void GenerateQueenMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        Span<(int fileDirection, int rankDirection)> queenDirections =
        [
            (1, 1), (-1, 1), (1, -1), (-1, -1),
            (1, 0), (-1, 0), (0, 1), (0, -1)
        ];
        var queens = position.WhiteToMove ? position.WhiteQueens : position.BlackQueens;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

        var currentQueens = queens;
        while (currentQueens != 0)
        {
            var from = TrailingZeroCount(currentQueens);
            var fromFile = from % 8;
            var fromRank = from / 8;

            for (var i = 0; i < queenDirections.Length; i++)
            {
                var currentFile = fromFile;
                var currentRank = fromRank;

                while (true)
                {
                    currentFile += queenDirections[i].fileDirection;
                    currentRank += queenDirections[i].rankDirection;

                    if (currentFile is < 0 or >= 8 || currentRank is < 0 or >= 8)
                    {
                        // Out of bounds
                        break;
                    }

                    var to = currentFile + currentRank * 8;
                    var toMask = Bitboard.Mask(to);

                    if ((friendlyPieces & toMask) != 0)
                    {
                        // Friendly piece at the target square
                        break;
                    }

                    if ((enemyPieces & toMask) != 0)
                    {
                        var captureMove = new Move(from, to, MoveType.Capture);
                        if (IsMoveLegal(position, captureMove))
                        {
                            movesBuffer[bufferIndex++] = captureMove;
                        }

                        break;
                    }

                    var quietMove = new Move(from, to, MoveType.Quiet);
                    if (IsMoveLegal(position, quietMove))
                    {
                        movesBuffer[bufferIndex++] = quietMove;
                    }
                }
            }

            // Remove the queen we just processed.
            currentQueens &= currentQueens - 1;
        }
    }

    private void GenerateKingMoves(Position position, ref int bufferIndex, Span<Move> movesBuffer)
    {
        var king = position.WhiteToMove ? position.WhiteKing : position.BlackKing;
        var friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
        var enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

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
                    move = new Move(from, to, MoveType.Capture);
                }
                else
                {
                    move = new Move(from, to, MoveType.Quiet);
                }

                if (IsMoveLegal(position, move))
                {
                    movesBuffer[bufferIndex++] = move;
                }
            }
        }

        // Castling moves
        if (position.WhiteToMove)
        {
            if (from != 4) return;

            // --- White kingside castling (e1 -> g1) ---
            if ((position.CastlingRights & 0b0001) != 0)
            {
                // Squares f1 (5) and g1 (6) must be empty.
                const ulong kingsideEmptySquares = (1UL << 5) | (1UL << 6);
                if ((position.AllPieces & kingsideEmptySquares) == 0 &&
                    // The king's start square and the squares it passes through must not be attacked.
                    !position.IsSquareAttacked(4, false) &&
                    !position.IsSquareAttacked(5, false) &&
                    !position.IsSquareAttacked(6, false))
                {
                    // Add the kingside castling move (king moves from 4 to 6).
                    movesBuffer[bufferIndex++] = new Move(4, 6, MoveType.Castling);
                }
            }

            // --- White queenside castling (e1 -> c1) ---
            if ((position.CastlingRights & 0b0010) != 0)
            {
                // Squares between the king and rook must be empty: b1 (1), c1 (2), and d1 (3).
                const ulong queensideEmptySquares = (1UL << 1) | (1UL << 2) | (1UL << 3);
                if ((position.AllPieces & queensideEmptySquares) == 0 &&
                    !position.IsSquareAttacked(4, false) &&
                    !position.IsSquareAttacked(3, false) && // d1 must not be attacked
                    !position.IsSquareAttacked(2, false))   // c1 must not be attacked
                {
                    // Add the queenside castling move (king moves from 4 to 2).
                    movesBuffer[bufferIndex++] = new Move(4, 2, MoveType.Castling);
                }
            }
        }
        else
        {
            if (from != 60) return;

            // --- Black kingside castling (e8 -> g8) ---
            if ((position.CastlingRights & 0b0100) != 0)
            {
                // Squares f8 (61) and g8 (62) must be empty.
                var kingsideEmptySquares = (1UL << 61) | (1UL << 62);
                if ((position.AllPieces & kingsideEmptySquares) == 0 &&
                    !position.IsSquareAttacked(60, true) &&
                    !position.IsSquareAttacked(61, true) &&
                    !position.IsSquareAttacked(62, true))
                {
                    // Add the kingside castling move (king moves from 60 to 62).
                    movesBuffer[bufferIndex++] = new Move(60, 62, MoveType.Castling);
                }
            }

            // --- Black queenside castling (e8 -> c8) ---
            if ((position.CastlingRights & 0b1000) != 0)
            {
                // Squares between king and rook: b8 (57), c8 (58), and d8 (59) must be empty.
                var queensideEmptySquares = (1UL << 57) | (1UL << 58) | (1UL << 59);
                if ((position.AllPieces & queensideEmptySquares) == 0 &&
                    !position.IsSquareAttacked(60, true) &&
                    !position.IsSquareAttacked(59, true) &&
                    !position.IsSquareAttacked(58, true))
                {
                    // Add the queenside castling move (king moves from 60 to 58).
                    movesBuffer[bufferIndex++] = new Move(60, 58, MoveType.Castling);
                }
            }
        }
    }

    private bool IsMoveLegal(Position position, Move move)
    {
        position.MakeMove(move);
        var kingSquare = TrailingZeroCount(position.WhiteToMove ? position.BlackKing : position.WhiteKing);
        var isLegal = !position.IsSquareAttacked(kingSquare, position.WhiteToMove);
        position.UndoMove();
        return isLegal;
    }
}
