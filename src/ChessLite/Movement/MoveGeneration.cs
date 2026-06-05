using System.Runtime.CompilerServices;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Movement;

internal static class MoveGeneration
{
    internal static int GenerateLegalMoves(Position position, Span<Move> legalMovesBuffer)
    {
        // Analyze position state once
        var state = AnalyzePosition(position);

        // Double check: only king can move
        if (state.CheckCount > 1)
        {
            return GenerateKingMovesOnly(position, legalMovesBuffer);
        }

        // Single check: only evasions (capture checker or block)
        if (state.CheckCount == 1)
        {
            return GenerateCheckEvasions(position, legalMovesBuffer, state.CheckEvasionMask);
        }

        // Normal position (no check): optimized generation
        return GenerateNormalMoves(position, legalMovesBuffer);
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
                var leftCapturedPieceType = DetermineCapturedPieceType(position, leftCaptureTo);

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
                var rightCapturedPieceType = DetermineCapturedPieceType(position, rightCaptureTo);
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
    private static PieceType DetermineCapturedPieceType(Position position, Square to)
    {
        var pieceType = position.Mailbox[(int)to];
        return pieceType is PieceType.WhiteKing or PieceType.BlackKing ? PieceType.None : pieceType;
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
                var capturedPieceType = DetermineCapturedPieceType(position, to);
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
                var capturedPieceType = DetermineCapturedPieceType(position, to);
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
                var capturedPieceType = DetermineCapturedPieceType(position, to);
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
                var capturedPieceType = DetermineCapturedPieceType(position, to);
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
                var capturedPieceType = DetermineCapturedPieceType(position, to);
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

    /// <summary>
    /// Holds pre-computed position characteristics for optimized legal move generation.
    /// </summary>
    private struct PositionState
    {
        public bool InCheck;
        public int CheckCount;
        public Square CheckerSquare;  // Only valid when CheckCount == 1
        public Bitboard CheckEvasionMask;  // Capture checker OR block squares (only valid when CheckCount == 1)
    }

    /// <summary>
    /// Analyzes the current position to determine check state and compute evasion masks.
    /// This is computed once per position to avoid redundant checks during move generation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PositionState AnalyzePosition(Position position)
    {
        var state = new PositionState();
        var kingSquare = position.WhiteToMove
            ? position.WhiteKing.GetFirstSquare()
            : position.BlackKing.GetFirstSquare();

        // Check if king is in check (uses cached attack bitboards from Position)
        state.InCheck = IsSquareAttacked(position, kingSquare, !position.WhiteToMove);

        if (state.InCheck)
        {
            // Find all pieces attacking the king
            var checkers = LegalityChecker.FindAttackingPieces(position, kingSquare, !position.WhiteToMove);
            state.CheckCount = checkers.Count();

            if (state.CheckCount == 1)
            {
                state.CheckerSquare = checkers.GetFirstSquare();
                state.CheckEvasionMask = ComputeCheckEvasionMask(position, kingSquare, state.CheckerSquare);
            }
        }

        return state;
    }

    /// <summary>
    /// Computes the set of squares where a piece can move to resolve a single check.
    /// This includes capturing the checking piece or blocking the attack ray.
    /// For en passant, also includes the en passant target square if it captures the checker.
    /// </summary>
    private static Bitboard ComputeCheckEvasionMask(Position position, Square kingSquare, Square checkerSquare)
    {
        // Can always capture the checker
        var mask = Bitboard.Mask(checkerSquare);

        // Can block if it's a sliding piece (bishop, rook, or queen)
        var checkerPieceType = LegalityChecker.GetPieceTypeAtSquare(position, checkerSquare);
        if (LegalityChecker.IsSlidingPiece(checkerPieceType))
        {
            // Add all squares between king and checker
            mask |= AttackTables.RayBetween[(int)kingSquare][(int)checkerSquare];
        }

        // Special case: if en passant target is set and the checker is a pawn,
        // en passant capture might resolve the check
        if (position.EnPassantTarget != Square.None)
        {
            var isWhite = position.WhiteToMove;
            var enPassantCapturedSquare = position.EnPassantTarget + (isWhite ? -8 : 8);
            
            // If the checker is the pawn that can be captured en passant
            if (enPassantCapturedSquare == checkerSquare)
            {
                mask |= Bitboard.Mask(position.EnPassantTarget);
            }
        }

        return mask;
    }

    /// <summary>
    /// Checks if a move by a pinned piece stays along the pin ray (king-piece-pinner line).
    /// This is inlined from LegalityChecker for performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Diagonal pin - check if the move maintains the same slope
        var fromKingFileDelta = pieceFile - kingFile;
        var fromKingRankDelta = pieceRank - kingRank;
        var toKingFileDelta = targetFile - kingFile;
        var toKingRankDelta = targetRank - kingRank;

        // The slopes must be equal for the move to be along the pin ray
        return Math.Abs(toKingFileDelta) == Math.Abs(toKingRankDelta) &&
               toKingFileDelta * fromKingFileDelta >= 0 &&  // Same file direction or through king
               toKingRankDelta * fromKingRankDelta >= 0;    // Same rank direction or through king
    }

    /// <summary>
    /// Generates legal moves when the king is in double check.
    /// Only king moves can resolve a double check.
    /// </summary>
    private static int GenerateKingMovesOnly(Position position, Span<Move> legalMovesBuffer)
    {
        var moveCount = 0;

        // Generate only king pseudo-legal moves
        Span<Move> kingMoves = stackalloc Move[8];  // King has maximum 8 possible moves
        GenerateKingMoves(position, ref moveCount, kingMoves);

        // Filter by enemy attacks (without our king)
        var enemyAttacks = position.WhiteToMove
            ? position.BlackAttacksWithoutWhiteKing
            : position.WhiteAttacksWithoutBlackKing;

        var legalCount = 0;
        for (var i = 0; i < moveCount; i++)
        {
            if (!enemyAttacks.Intersects(kingMoves[i].To))
            {
                legalMovesBuffer[legalCount++] = kingMoves[i];
            }
        }

        return legalCount;
    }

    /// <summary>
    /// Generates legal moves when the king is in single check.
    /// Only moves that capture the checker or block the attack are legal (plus king moves).
    /// </summary>
    private static int GenerateCheckEvasions(Position position, Span<Move> legalMovesBuffer, Bitboard evasionMask)
    {
        var moveCount = 0;

        // Generate all pseudo-legal moves
        Span<Move> pseudoMoves = stackalloc Move[256];
        var pseudoCount = GeneratePseudoLegalMoves(position, pseudoMoves);

        // Get enemy attacks (without our king for king move validation)
        var enemyAttacks = position.WhiteToMove
            ? position.BlackAttacksWithoutWhiteKing
            : position.WhiteAttacksWithoutBlackKing;

        var kingSquare = position.WhiteToMove
            ? position.WhiteKing.GetFirstSquare()
            : position.BlackKing.GetFirstSquare();

        // Filter pseudo-legal moves
        for (var i = 0; i < pseudoCount; i++)
        {
            var move = pseudoMoves[i];

            // King moves: must not land on attacked square
            if (move.PieceType is PieceType.WhiteKing or PieceType.BlackKing)
            {
                if (!enemyAttacks.Intersects(move.To))
                {
                    legalMovesBuffer[moveCount++] = move;
                }
            }
            // Non-king pieces: must capture checker or block, and respect pins
            else if (evasionMask.Intersects(move.To))
            {
                // Check if piece is pinned
                if (position.PinnedPieces.Intersects(move.From))
                {
                    // For en passant, need both pin ray AND horizontal pin checks
                    if (move.SpecialMoveType == SpecialMoveType.EnPassant)
                    {
                        if (IsMovingAlongPinRay(move, kingSquare) &&
                            !LegalityChecker.IsEnPassantPinned(position, move, kingSquare))
                        {
                            legalMovesBuffer[moveCount++] = move;
                        }
                    }
                    else if (IsMovingAlongPinRay(move, kingSquare))
                    {
                        legalMovesBuffer[moveCount++] = move;
                    }
                }
                // Unpinned pieces - but still need en passant check
                else if (move.SpecialMoveType == SpecialMoveType.EnPassant)
                {
                    if (!LegalityChecker.IsEnPassantPinned(position, move, kingSquare))
                    {
                        legalMovesBuffer[moveCount++] = move;
                    }
                }
                // Unpinned non-en-passant: legal
                else
                {
                    legalMovesBuffer[moveCount++] = move;
                }
            }
        }

        return moveCount;
    }

    /// <summary>
    /// Generates legal moves for positions where the king is not in check.
    /// Optimizes by skipping legality checks for unpinned non-king pieces.
    /// </summary>
    private static int GenerateNormalMoves(Position position, Span<Move> legalMovesBuffer)
    {
        var moveCount = 0;

        // Generate all pseudo-legal moves
        Span<Move> pseudoMoves = stackalloc Move[256];
        var pseudoCount = GeneratePseudoLegalMoves(position, pseudoMoves);

        // Get enemy attacks (without our king for king move validation)
        var enemyAttacks = position.WhiteToMove
            ? position.BlackAttacksWithoutWhiteKing
            : position.WhiteAttacksWithoutBlackKing;

        var kingSquare = position.WhiteToMove
            ? position.WhiteKing.GetFirstSquare()
            : position.BlackKing.GetFirstSquare();

        // Filter pseudo-legal moves based on piece state
        for (var i = 0; i < pseudoCount; i++)
        {
            var move = pseudoMoves[i];

            // King moves: check that we don't move into an attacked square
            if (move.PieceType is PieceType.WhiteKing or PieceType.BlackKing)
            {
                if (!enemyAttacks.Intersects(move.To))
                {
                    legalMovesBuffer[moveCount++] = move;
                }
            }
            // Pinned pieces: must move along the pin ray
            else if (position.PinnedPieces.Intersects(move.From))
            {
                // For en passant, need to check BOTH pin ray AND horizontal pin
                if (move.SpecialMoveType == SpecialMoveType.EnPassant)
                {
                    // Must satisfy pin ray constraint AND not create horizontal discovered check
                    if (IsMovingAlongPinRay(move, kingSquare) &&
                        !LegalityChecker.IsEnPassantPinned(position, move, kingSquare))
                    {
                        legalMovesBuffer[moveCount++] = move;
                    }
                }
                else if (IsMovingAlongPinRay(move, kingSquare))
                {
                    legalMovesBuffer[moveCount++] = move;
                }
            }
            // En passant always needs horizontal pin check (even if pawn itself isn't pinned)
            else if (move.SpecialMoveType == SpecialMoveType.EnPassant)
            {
                if (!LegalityChecker.IsEnPassantPinned(position, move, kingSquare))
                {
                    legalMovesBuffer[moveCount++] = move;
                }
            }
            // Unpinned non-king pieces: always legal in non-check positions!
            else
            {
                legalMovesBuffer[moveCount++] = move;
            }
        }

        return moveCount;
    }
}
