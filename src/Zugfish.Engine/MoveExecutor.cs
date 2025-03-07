using Zugfish.Engine.Extensions;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public class MoveExecutor
{
    private readonly Stack<MoveHistory> _moveHistory = new(256);

    public void ClearMoveHistory()
    {
        _moveHistory.Clear();
    }

    // TODO: pass properties in to avoid recalculating masks
    public void MakeMove(Position position, Move move)
    {
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
        UpdateCombinedBitboards(position);
        UpdateAttacks(position);

        position.WhiteToMove = !position.WhiteToMove;
        position.ZobristHash = Zobrist.ComputeHash(position);
        position.RepetitionTable[position.CurrentPly++] = position.ZobristHash;
    }

    private void SaveMoveHistory(Position position, Move move)
    {
        var moveHistory = new MoveHistory
        {
            Move = move,
            PreviousCastlingRights = position.CastlingRights,
            PreviousEnPassantTarget = position.EnPassantTarget,
            PreviousHalfmoveClock = position.HalfmoveClock,
            PreviousZobristHash = position.ZobristHash,
            PreviousWhiteAttacks = position.WhiteAttacks,
            PreviousBlackAttacks = position.BlackAttacks
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
                _ => throw new ArgumentOutOfRangeException()
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
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        ref var promotedPieceBitboard = ref position.GetPieceBitboard(promotedPieceType);
        promotedPieceBitboard = promotedPieceBitboard.SetSquare(move.To);
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

    private static void UpdateCombinedBitboards(Position position)
    {
        position.WhitePieces = position.WhitePawns | position.WhiteKnights | position.WhiteBishops |
                               position.WhiteRooks | position.WhiteQueens | position.WhiteKing;
        position.BlackPieces = position.BlackPawns | position.BlackKnights | position.BlackBishops |
                               position.BlackRooks | position.BlackQueens | position.BlackKing;
        position.AllPieces = position.WhitePieces | position.BlackPieces;
    }

    private static void UpdateAttacks(Position position)
    {
        position.WhiteAttacks = AttackGeneration.CalculateAttacks(position, forWhite: true);
        position.BlackAttacks = AttackGeneration.CalculateAttacks(position, forWhite: false);
    }

    public void UndoMove(Position position)
    {
        var moveHistory = _moveHistory.Pop();
        var previousMove = moveHistory.Move;

        if (previousMove.SpecialMoveType != SpecialMoveType.None)
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

        UpdateCombinedBitboards(position);
        position.WhiteToMove = !position.WhiteToMove;
        if (position.CurrentPly > 0) position.CurrentPly--;
        position.ZobristHash = moveHistory.PreviousZobristHash;
        position.WhiteAttacks = moveHistory.PreviousWhiteAttacks;
        position.BlackAttacks = moveHistory.PreviousBlackAttacks;
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
                _ => throw new ArgumentOutOfRangeException()
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
                _ => throw new ArgumentOutOfRangeException()
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
        }
    }
}
