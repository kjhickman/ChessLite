using System.Runtime.CompilerServices;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Movement;

internal class MoveExecutor
{
    private readonly Stack<MoveHistory> _moveHistory = new(256);

    internal void ClearMoveHistory()
    {
        _moveHistory.Clear();
    }

    internal MoveExecutor Clone()
    {
        var clone = new MoveExecutor();
        foreach (var moveHistory in _moveHistory.Reverse())
        {
            clone._moveHistory.Push(moveHistory);
        }

        return clone;
    }

    // TODO: pass properties in to avoid recalculating masks
    internal void MakeMove(Position position, Move move)
    {
        var previousHash = position.ZobristHash;
        var previousCastlingRights = position.CastlingRights;
        var previousEnPassantTarget = position.EnPassantTarget;

        SaveMoveHistory(position, move);

        if (move.SpecialMoveType != SpecialMoveType.None)
        {
            HandleSpecialMove(position, move);
        }
        else if (move.PromotedPieceType != PromotedPieceType.None)
        {
            HandlePromotionMove(position, move);
        }
        else
        {
            HandleRegularMove(position, move);
        }

        if (move.SpecialMoveType != SpecialMoveType.DoublePawnPush) position.EnPassantTarget = Square.None;
        UpdateCastlingRights(position, move);
        UpdateHalfmoveClock(position, move);
        UpdateFullmoveNumber(position);
        UpdateCombinedBitboards(position);
        position.UpdateAttacks();
        position.WhiteToMove = !position.WhiteToMove;
        position.UpdatePinnedPieces(); // Must be called after toggling the turn
        position.ZobristHash = UpdateZobristHash(previousHash, move, previousCastlingRights, previousEnPassantTarget, position);
    }

    internal void MakeNullMove(Position position)
    {
        var previousHash = position.ZobristHash;
        var previousEnPassantTarget = position.EnPassantTarget;

        SaveMoveHistory(position, Move.NullMove);

        position.EnPassantTarget = Square.None;
        position.HalfmoveClock++;
        UpdateFullmoveNumber(position);
        position.WhiteToMove = !position.WhiteToMove;
        position.UpdatePinnedPieces();
        position.ZobristHash = UpdateNullMoveZobristHash(previousHash, previousEnPassantTarget);
    }

    internal IEnumerable<Move> GetMoveHistory()
    {
        return _moveHistory.Select(x => x.Move).Reverse();
    }

    private void SaveMoveHistory(Position position, Move move)
    {
        var moveHistory = new MoveHistory
        {
            Move = move,
            PreviousCastlingRights = position.CastlingRights,
            PreviousEnPassantTarget = position.EnPassantTarget,
            PreviousHalfmoveClock = position.HalfmoveClock,
            PreviousFullmoveNumber = position.FullmoveNumber,
            PreviousZobristHash = position.ZobristHash,
            PreviousWhiteAttacks = position.WhiteAttacks,
            PreviousWhiteAttacksWithoutBlackKing = position.WhiteAttacksWithoutBlackKing,
            PreviousWhitePawnAttacks = position.WhitePawnAttacks,
            PreviousWhiteKnightAttacks = position.WhiteKnightAttacks,
            PreviousWhiteKingAttacks = position.WhiteKingAttacks,
            PreviousBlackAttacks = position.BlackAttacks,
            PreviousBlackAttacksWithoutWhiteKing = position.BlackAttacksWithoutWhiteKing,
            PreviousBlackPawnAttacks = position.BlackPawnAttacks,
            PreviousBlackKnightAttacks = position.BlackKnightAttacks,
            PreviousBlackKingAttacks = position.BlackKingAttacks,
            PreviousPinnedPieces = position.PinnedPieces,
        };
        _moveHistory.Push(moveHistory);
    }

    private static void HandleSpecialMove(Position position, Move move)
    {
        switch (move.SpecialMoveType)
        {
            case SpecialMoveType.DoublePawnPush:
                HandleDoublePawnPush(position, move);
                break;
            case SpecialMoveType.EnPassant:
                HandleEnPassant(position, move);
                break;
            case SpecialMoveType.ShortCastle:
                HandleShortCastle(position, move);
                break;
            case SpecialMoveType.LongCastle:
                HandleLongCastle(position, move);
                break;
            case SpecialMoveType.None:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void HandleDoublePawnPush(Position position, Move move)
    {
        // Move piece
        ref var pieceBitboard = ref position.GetPieceBitboard(move.PieceType);
        pieceBitboard = pieceBitboard.MoveSquare(move.From, move.To);

        // Set en passant target
        var offset = move.PieceType == PieceType.WhitePawn ? -8 : 8;
        position.EnPassantTarget = move.To + offset;

        // Update mailbox
        position.Mailbox[(int)move.From] = PieceType.None;
        position.Mailbox[(int)move.To] = move.PieceType;
    }

    private static void HandleEnPassant(Position position, Move move)
    {
        // Move piece
        ref var pieceBitboard = ref position.GetPieceBitboard(move.PieceType);
        pieceBitboard = pieceBitboard.MoveSquare(move.From, move.To);

        // Remove captured piece
        ref var capturedPiece = ref position.GetPieceBitboard(move.CapturedPieceType);
        var offset = move.PieceType == PieceType.WhitePawn ? -8 : 8;
        var capturedPieceSquare = position.EnPassantTarget + offset;
        capturedPiece = capturedPiece.ClearSquare(capturedPieceSquare);

        // Update mailbox
        position.Mailbox[(int)move.From] = PieceType.None;
        position.Mailbox[(int)move.To] = move.PieceType;
        position.Mailbox[(int)capturedPieceSquare] = PieceType.None;
    }

    private static void HandleShortCastle(Position position, Move move)
    {
        // TODO: use constants for these known bitboard masks
        if (move.PieceType == PieceType.WhiteKing)
        {
            // Move white king
            ref var whiteKingBitboard = ref position.WhiteKing;
            whiteKingBitboard = whiteKingBitboard.ClearSquares(Constants.E1Mask);
            whiteKingBitboard = whiteKingBitboard.SetSquares(Constants.G1Mask);

            // Move white rook
            ref var whiteRookBitboard = ref position.WhiteRooks;
            whiteRookBitboard = whiteRookBitboard.ClearSquares(Constants.H1Mask);
            whiteRookBitboard = whiteRookBitboard.SetSquares(Constants.F1Mask);

            // Update mailbox
            position.Mailbox[(int)Square.e1] = PieceType.None;
            position.Mailbox[(int)Square.g1] = PieceType.WhiteKing;
            position.Mailbox[(int)Square.h1] = PieceType.None;
            position.Mailbox[(int)Square.f1] = PieceType.WhiteRook;
        }
        else
        {
            // Move black king
            ref var blackKingBitboard = ref position.BlackKing;
            blackKingBitboard = blackKingBitboard.ClearSquares(Constants.E8Mask);
            blackKingBitboard = blackKingBitboard.SetSquares(Constants.G8Mask);

            // Move black rook
            ref var blackRookBitboard = ref position.BlackRooks;
            blackRookBitboard = blackRookBitboard.ClearSquares(Constants.H8Mask);
            blackRookBitboard = blackRookBitboard.SetSquares(Constants.F8Mask);

            // Update mailbox
            position.Mailbox[(int)Square.e8] = PieceType.None;
            position.Mailbox[(int)Square.g8] = PieceType.BlackKing;
            position.Mailbox[(int)Square.h8] = PieceType.None;
            position.Mailbox[(int)Square.f8] = PieceType.BlackRook;
        }
    }

    private static void HandleLongCastle(Position position, Move move)
    {
        // TODO: use constants for these known bitboard masks
        if (move.PieceType == PieceType.WhiteKing)
        {
            // Move white king
            ref var whiteKingBitboard = ref position.WhiteKing;
            whiteKingBitboard = whiteKingBitboard.ClearSquares(Constants.E1Mask);
            whiteKingBitboard = whiteKingBitboard.SetSquares(Constants.C1Mask);

            // Move white rook
            ref var whiteRookBitboard = ref position.WhiteRooks;
            whiteRookBitboard = whiteRookBitboard.ClearSquares(Constants.A1Mask);
            whiteRookBitboard = whiteRookBitboard.SetSquares(Constants.D1Mask);

            // Update mailbox
            position.Mailbox[(int)Square.e1] = PieceType.None;
            position.Mailbox[(int)Square.c1] = PieceType.WhiteKing;
            position.Mailbox[(int)Square.a1] = PieceType.None;
            position.Mailbox[(int)Square.d1] = PieceType.WhiteRook;
        }
        else
        {
            // Move black king
            ref var blackKingBitboard = ref position.BlackKing;
            blackKingBitboard = blackKingBitboard.ClearSquares(Constants.E8Mask);
            blackKingBitboard = blackKingBitboard.SetSquares(Constants.C8Mask);

            // Move black rook
            ref var blackRookBitboard = ref position.BlackRooks;
            blackRookBitboard = blackRookBitboard.ClearSquares(Constants.A8Mask);
            blackRookBitboard = blackRookBitboard.SetSquares(Constants.D8Mask);

            // Update mailbox
            position.Mailbox[(int)Square.e8] = PieceType.None;
            position.Mailbox[(int)Square.c8] = PieceType.BlackKing;
            position.Mailbox[(int)Square.a8] = PieceType.None;
            position.Mailbox[(int)Square.d8] = PieceType.BlackRook;
        }
    }

    private static void HandlePromotionMove(Position position, Move move)
    {
        if (move.IsCapture)
        {
            // Remove captured piece
            ref var capturedPieceType = ref position.GetPieceBitboard(move.CapturedPieceType);
            capturedPieceType = capturedPieceType.ClearSquare(move.To);
        }

        // Remove pawn
        ref var pieceBitboard = ref position.GetPieceBitboard(move.PieceType);
        pieceBitboard = pieceBitboard.ClearSquare(move.From);

        // Add promoted piece
        PieceType promotedPieceType; // todo: make 'PromotedPieceType' include color, shouldn't need to use flags
        if (move.PieceType == PieceType.WhitePawn)
        {
            promotedPieceType = move.PromotedPieceType switch
            {
                PromotedPieceType.Queen => PieceType.WhiteQueen,
                PromotedPieceType.Rook => PieceType.WhiteRook,
                PromotedPieceType.Bishop => PieceType.WhiteBishop,
                PromotedPieceType.Knight => PieceType.WhiteKnight,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        else
        {
            promotedPieceType = move.PromotedPieceType switch
            {
                PromotedPieceType.Queen => PieceType.BlackQueen,
                PromotedPieceType.Rook => PieceType.BlackRook,
                PromotedPieceType.Bishop => PieceType.BlackBishop,
                PromotedPieceType.Knight => PieceType.BlackKnight,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        ref var promotedPieceBitboard = ref position.GetPieceBitboard(promotedPieceType);
        promotedPieceBitboard = promotedPieceBitboard.SetSquare(move.To);

        // Update mailbox
        position.Mailbox[(int)move.From] = PieceType.None;
        position.Mailbox[(int)move.To] = promotedPieceType;
    }

    private static void HandleRegularMove(Position position, Move move)
    {
        // Move piece
        ref var pieceBitboard = ref position.GetPieceBitboard(move.PieceType);
        pieceBitboard = pieceBitboard.MoveSquare(move.From, move.To);

        if (move.IsCapture)
        {
            // Remove captured piece
            ref var capturedPieceType = ref position.GetPieceBitboard(move.CapturedPieceType);
            capturedPieceType = capturedPieceType.ClearSquare(move.To);
        }

        // Update mailbox
        position.Mailbox[(int)move.From] = PieceType.None;
        position.Mailbox[(int)move.To] = move.PieceType;
    }

    private static void UpdateCastlingRights(Position position, Move move)
    {
        // Update castling rights based on movement
        if (move.PieceType == PieceType.WhiteKing)
        {
            position.CastlingRights = position.CastlingRights.Remove(CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside);
        }
        else if (move.PieceType == PieceType.BlackKing)
        {
            position.CastlingRights = position.CastlingRights.Remove(CastlingRights.BlackKingside | CastlingRights.BlackQueenside);
        }
        else if (move.PieceType == PieceType.WhiteRook)
        {
            if (move.From == Square.h1)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.WhiteKingside);
            }
            else if (move.From == Square.a1)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.WhiteQueenside);
            }
        }
        else if (move.PieceType == PieceType.BlackRook)
        {
            if (move.From == Square.h8)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.BlackKingside);
            }
            else if (move.From == Square.a8)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.BlackQueenside);
            }
        }

        if (!move.IsCapture) return;

        // Update castling rights based on captures
        if (move.CapturedPieceType == PieceType.WhiteRook)
        {
            if (move.To == Square.h1)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.WhiteKingside);
            }
            else if (move.To == Square.a1)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.WhiteQueenside);
            }
        }
        else if (move.CapturedPieceType == PieceType.BlackRook)
        {
            if (move.To == Square.h8)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.BlackKingside);
            }
            else if (move.To == Square.a8)
            {
                position.CastlingRights = position.CastlingRights.Remove(CastlingRights.BlackQueenside);
            }
        }
    }

    private static void UpdateHalfmoveClock(Position position, Move move)
    {
        if (move.PieceType == PieceType.WhitePawn || move.PieceType == PieceType.BlackPawn || move.IsCapture)
        {
            position.HalfmoveClock = 0;
        }
        else
        {
            position.HalfmoveClock++;
        }
    }

    private static void UpdateFullmoveNumber(Position position)
    {
        if (!position.WhiteToMove)
        {
            position.FullmoveNumber++;
        }
    }

    private static void UpdateCombinedBitboards(Position position)
    {
        position.WhitePieces = position.WhitePawns | position.WhiteKnights | position.WhiteBishops |
                               position.WhiteRooks | position.WhiteQueens | position.WhiteKing;
        position.BlackPieces = position.BlackPawns | position.BlackKnights | position.BlackBishops |
                               position.BlackRooks | position.BlackQueens | position.BlackKing;
        position.AllPieces = position.WhitePieces | position.BlackPieces;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateZobristHash(
        ulong previousHash,
        Move move,
        CastlingRights previousCastlingRights,
        Square previousEnPassantTarget,
        Position position)
    {
        if (move.SpecialMoveType == SpecialMoveType.None
            && move.PromotedPieceType == PromotedPieceType.None
            && previousCastlingRights == position.CastlingRights
            && previousEnPassantTarget == Square.None
            && position.EnPassantTarget == Square.None)
        {
            return UpdateRegularMoveZobristHash(previousHash ^ Zobrist.SideToMoveKey, move);
        }

        var hash = previousHash;

        if (previousCastlingRights != position.CastlingRights)
        {
            hash ^= Zobrist.GetCastlingKey(previousCastlingRights);
            hash ^= Zobrist.GetCastlingKey(position.CastlingRights);
        }

        if (previousEnPassantTarget != Square.None)
        {
            hash ^= Zobrist.GetEnPassantKey(previousEnPassantTarget);
        }

        if (position.EnPassantTarget != Square.None)
        {
            hash ^= Zobrist.GetEnPassantKey(position.EnPassantTarget);
        }

        hash ^= Zobrist.SideToMoveKey;

        if (move.PromotedPieceType != PromotedPieceType.None)
        {
            return UpdatePromotionZobristHash(hash, move);
        }

        if (move.SpecialMoveType != SpecialMoveType.None)
        {
            return UpdateSpecialMoveZobristHash(hash, move);
        }

        return UpdateRegularMoveZobristHash(hash, move);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateNullMoveZobristHash(ulong previousHash, Square previousEnPassantTarget)
    {
        var hash = previousHash;

        if (previousEnPassantTarget != Square.None)
        {
            hash ^= Zobrist.GetEnPassantKey(previousEnPassantTarget);
        }

        hash ^= Zobrist.SideToMoveKey;
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateRegularMoveZobristHash(ulong hash, Move move)
    {
        hash ^= Zobrist.GetPieceKey(move.PieceType, move.From);
        hash ^= Zobrist.GetPieceKey(move.PieceType, move.To);

        if (move.IsCapture)
        {
            hash ^= Zobrist.GetPieceKey(move.CapturedPieceType, move.To);
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdatePromotionZobristHash(ulong hash, Move move)
    {
        hash ^= Zobrist.GetPieceKey(move.PieceType, move.From);
        hash ^= Zobrist.GetPieceKey(GetPromotedPieceType(move), move.To);

        if (move.IsCapture)
        {
            hash ^= Zobrist.GetPieceKey(move.CapturedPieceType, move.To);
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateSpecialMoveZobristHash(ulong hash, Move move)
    {
        return move.SpecialMoveType switch
        {
            SpecialMoveType.DoublePawnPush => UpdateRegularMoveZobristHash(hash, move),
            SpecialMoveType.EnPassant => UpdateEnPassantZobristHash(hash, move),
            SpecialMoveType.ShortCastle => UpdateCastleZobristHash(hash, move, isShortCastle: true),
            SpecialMoveType.LongCastle => UpdateCastleZobristHash(hash, move, isShortCastle: false),
            SpecialMoveType.None => hash,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateEnPassantZobristHash(ulong hash, Move move)
    {
        var capturedSquare = move.PieceType == PieceType.WhitePawn
            ? move.To - 8
            : move.To + 8;

        hash ^= Zobrist.GetPieceKey(move.PieceType, move.From);
        hash ^= Zobrist.GetPieceKey(move.PieceType, move.To);
        hash ^= Zobrist.GetPieceKey(move.CapturedPieceType, capturedSquare);
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong UpdateCastleZobristHash(ulong hash, Move move, bool isShortCastle)
    {
        hash ^= Zobrist.GetPieceKey(move.PieceType, move.From);
        hash ^= Zobrist.GetPieceKey(move.PieceType, move.To);

        var isWhite = move.PieceType == PieceType.WhiteKing;
        var rookPieceType = isWhite ? PieceType.WhiteRook : PieceType.BlackRook;
        var rookFrom = isWhite
            ? isShortCastle ? Square.h1 : Square.a1
            : isShortCastle ? Square.h8 : Square.a8;
        var rookTo = isWhite
            ? isShortCastle ? Square.f1 : Square.d1
            : isShortCastle ? Square.f8 : Square.d8;

        hash ^= Zobrist.GetPieceKey(rookPieceType, rookFrom);
        hash ^= Zobrist.GetPieceKey(rookPieceType, rookTo);
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PieceType GetPromotedPieceType(Move move)
    {
        return move.PieceType == PieceType.WhitePawn
            ? move.PromotedPieceType switch
            {
                PromotedPieceType.Queen => PieceType.WhiteQueen,
                PromotedPieceType.Rook => PieceType.WhiteRook,
                PromotedPieceType.Bishop => PieceType.WhiteBishop,
                PromotedPieceType.Knight => PieceType.WhiteKnight,
                _ => throw new ArgumentOutOfRangeException(),
            }
            : move.PromotedPieceType switch
            {
                PromotedPieceType.Queen => PieceType.BlackQueen,
                PromotedPieceType.Rook => PieceType.BlackRook,
                PromotedPieceType.Bishop => PieceType.BlackBishop,
                PromotedPieceType.Knight => PieceType.BlackKnight,
                _ => throw new ArgumentOutOfRangeException(),
            };
    }

    internal void UndoMove(Position position)
    {
        var moveHistory = _moveHistory.Pop();
        var previousMove = moveHistory.Move;

        if (previousMove == Move.NullMove)
        {
            // No board pieces moved; restore only saved state below.
        }
        else if (previousMove.SpecialMoveType != SpecialMoveType.None)
        {
            UndoSpecialMove(position, previousMove);
        }
        else if (previousMove.PromotedPieceType != PromotedPieceType.None)
        {
            UndoPromotionMove(position, previousMove);
        }
        else
        {
            UndoRegularMove(position, previousMove);
        }

        position.EnPassantTarget = moveHistory.PreviousEnPassantTarget;
        position.CastlingRights = moveHistory.PreviousCastlingRights;
        position.HalfmoveClock = moveHistory.PreviousHalfmoveClock;
        position.FullmoveNumber = moveHistory.PreviousFullmoveNumber;

        UpdateCombinedBitboards(position);

        position.WhiteToMove = !position.WhiteToMove;
        position.ZobristHash = moveHistory.PreviousZobristHash;
        position.WhiteAttacks = moveHistory.PreviousWhiteAttacks;
        position.WhiteAttacksWithoutBlackKing = moveHistory.PreviousWhiteAttacksWithoutBlackKing;
        position.WhitePawnAttacks = moveHistory.PreviousWhitePawnAttacks;
        position.WhiteKnightAttacks = moveHistory.PreviousWhiteKnightAttacks;
        position.WhiteKingAttacks = moveHistory.PreviousWhiteKingAttacks;
        position.BlackAttacks = moveHistory.PreviousBlackAttacks;
        position.BlackAttacksWithoutWhiteKing = moveHistory.PreviousBlackAttacksWithoutWhiteKing;
        position.BlackPawnAttacks = moveHistory.PreviousBlackPawnAttacks;
        position.BlackKnightAttacks = moveHistory.PreviousBlackKnightAttacks;
        position.BlackKingAttacks = moveHistory.PreviousBlackKingAttacks;
        position.PinnedPieces = moveHistory.PreviousPinnedPieces;
    }

    private static void UndoSpecialMove(Position position, Move move)
    {
        switch (move.SpecialMoveType)
        {
            case SpecialMoveType.DoublePawnPush:
                UndoRegularMove(position, move); // Same logic as regular move
                break;
            case SpecialMoveType.EnPassant:
                UndoEnPassant(position, move);
                break;
            case SpecialMoveType.ShortCastle:
                UndoShortCastle(position, move);
                break;
            case SpecialMoveType.LongCastle:
                UndoLongCastle(position, move);
                break;
            case SpecialMoveType.None:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void UndoPromotionMove(Position position, Move previousMove)
    {
        // Remove promoted piece
        PieceType promotedPieceType;
        if (previousMove.PieceType == PieceType.WhitePawn)
        {
            promotedPieceType = previousMove.PromotedPieceType switch
            {
                PromotedPieceType.Queen => PieceType.WhiteQueen,
                PromotedPieceType.Rook => PieceType.WhiteRook,
                PromotedPieceType.Bishop => PieceType.WhiteBishop,
                PromotedPieceType.Knight => PieceType.WhiteKnight,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        else
        {
            promotedPieceType = previousMove.PromotedPieceType switch
            {
                PromotedPieceType.Queen => PieceType.BlackQueen,
                PromotedPieceType.Rook => PieceType.BlackRook,
                PromotedPieceType.Bishop => PieceType.BlackBishop,
                PromotedPieceType.Knight => PieceType.BlackKnight,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        ref var promotedPieceBitboard = ref position.GetPieceBitboard(promotedPieceType);
        promotedPieceBitboard = promotedPieceBitboard.ClearSquare(previousMove.To);

        // Restore pawn
        ref var pieceBitboard = ref position.GetPieceBitboard(previousMove.PieceType);
        pieceBitboard = pieceBitboard.SetSquare(previousMove.From);

        if (previousMove.IsCapture)
        {
            // Restore captured piece
            ref var capturedPieceType = ref position.GetPieceBitboard(previousMove.CapturedPieceType);
            capturedPieceType = capturedPieceType.SetSquare(previousMove.To);
        }

        // Update mailbox
        position.Mailbox[(int)previousMove.From] = previousMove.PieceType;
        position.Mailbox[(int)previousMove.To] = previousMove.IsCapture ? previousMove.CapturedPieceType : PieceType.None;
    }

    private static void UndoRegularMove(Position position, Move previousMove)
    {
        // Move piece back
        ref var pieceBitboard = ref position.GetPieceBitboard(previousMove.PieceType);
        pieceBitboard = pieceBitboard.MoveSquare(previousMove.To, previousMove.From);

        if (previousMove.IsCapture)
        {
            // Restore captured piece
            ref var capturedPieceType = ref position.GetPieceBitboard(previousMove.CapturedPieceType);
            capturedPieceType = capturedPieceType.SetSquare(previousMove.To);
        }

        // Update mailbox
        position.Mailbox[(int)previousMove.To] = previousMove.IsCapture ? previousMove.CapturedPieceType : PieceType.None;
        position.Mailbox[(int)previousMove.From] = previousMove.PieceType;
    }

    private static void UndoEnPassant(Position position, Move previousMove)
    {
        // Move piece back
        ref var pieceBitboard = ref position.GetPieceBitboard(previousMove.PieceType);
        pieceBitboard = pieceBitboard.MoveSquare(previousMove.To, previousMove.From);

        // Restore captured piece
        ref var capturedPieceType = ref position.GetPieceBitboard(previousMove.CapturedPieceType);
        var offset = previousMove.PieceType == PieceType.WhitePawn ? -8 : 8;
        var capturedPieceSquare = previousMove.To + offset;
        capturedPieceType = capturedPieceType.SetSquare(capturedPieceSquare);

        // Update mailbox
        position.Mailbox[(int)previousMove.From] = previousMove.PieceType;
        position.Mailbox[(int)previousMove.To] = PieceType.None;
        position.Mailbox[(int)capturedPieceSquare] = previousMove.CapturedPieceType;
    }

    private static void UndoShortCastle(Position position, Move previousMove)
    {
        if (previousMove.PieceType == PieceType.WhiteKing)
        {
            // Move white king back
            ref var whiteKingBitboard = ref position.WhiteKing;
            whiteKingBitboard = whiteKingBitboard.ClearSquares(Constants.G1Mask);
            whiteKingBitboard = whiteKingBitboard.SetSquares(Constants.E1Mask);

            // Move white rook back
            ref var whiteRookBitboard = ref position.WhiteRooks;
            whiteRookBitboard = whiteRookBitboard.ClearSquares(Constants.F1Mask);
            whiteRookBitboard = whiteRookBitboard.SetSquares(Constants.H1Mask);

            // Update mailbox
            position.Mailbox[(int)Square.g1] = PieceType.None;
            position.Mailbox[(int)Square.e1] = PieceType.WhiteKing;
            position.Mailbox[(int)Square.f1] = PieceType.None;
            position.Mailbox[(int)Square.h1] = PieceType.WhiteRook;
        }
        else
        {
            // Move black king back
            ref var blackKingBitboard = ref position.BlackKing;
            blackKingBitboard = blackKingBitboard.ClearSquares(Constants.G8Mask);
            blackKingBitboard = blackKingBitboard.SetSquares(Constants.E8Mask);

            // Move black rook back
            ref var blackRookBitboard = ref position.BlackRooks;
            blackRookBitboard = blackRookBitboard.ClearSquares(Constants.F8Mask);
            blackRookBitboard = blackRookBitboard.SetSquares(Constants.H8Mask);

            // Update mailbox
            position.Mailbox[(int)Square.g8] = PieceType.None;
            position.Mailbox[(int)Square.e8] = PieceType.BlackKing;
            position.Mailbox[(int)Square.f8] = PieceType.None;
            position.Mailbox[(int)Square.h8] = PieceType.BlackRook;
        }
    }

    private static void UndoLongCastle(Position position, Move previousMove)
    {
        if (previousMove.PieceType == PieceType.WhiteKing)
        {
            // Move white king back
            ref var whiteKingBitboard = ref position.WhiteKing;
            whiteKingBitboard = whiteKingBitboard.ClearSquares(Constants.C1Mask);
            whiteKingBitboard = whiteKingBitboard.SetSquares(Constants.E1Mask);

            // Move white rook back
            ref var whiteRookBitboard = ref position.WhiteRooks;
            whiteRookBitboard = whiteRookBitboard.ClearSquares(Constants.D1Mask);
            whiteRookBitboard = whiteRookBitboard.SetSquares(Constants.A1Mask);

            // Update mailbox
            position.Mailbox[(int)Square.c1] = PieceType.None;
            position.Mailbox[(int)Square.e1] = PieceType.WhiteKing;
            position.Mailbox[(int)Square.d1] = PieceType.None;
            position.Mailbox[(int)Square.a1] = PieceType.WhiteRook;
        }
        else
        {
            // Move black king back
            ref var blackKingBitboard = ref position.BlackKing;
            blackKingBitboard = blackKingBitboard.ClearSquares(Constants.C8Mask);
            blackKingBitboard = blackKingBitboard.SetSquares(Constants.E8Mask);

            // Move black rook back
            ref var blackRookBitboard = ref position.BlackRooks;
            blackRookBitboard = blackRookBitboard.ClearSquares(Constants.D8Mask);
            blackRookBitboard = blackRookBitboard.SetSquares(Constants.A8Mask);

            // Update mailbox
            position.Mailbox[(int)Square.c8] = PieceType.None;
            position.Mailbox[(int)Square.e8] = PieceType.BlackKing;
            position.Mailbox[(int)Square.d8] = PieceType.None;
            position.Mailbox[(int)Square.a8] = PieceType.BlackRook;
        }
    }
}
