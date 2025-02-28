using System.Numerics;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public class Position
{
    #region Fields and Properties

    private Bitboard _whitePawns;
    private Bitboard _whiteKnights;
    private Bitboard _whiteBishops;
    private Bitboard _whiteRooks;
    private Bitboard _whiteQueens;
    private Bitboard _whiteKing;

    private Bitboard _blackPawns;
    private Bitboard _blackKnights;
    private Bitboard _blackBishops;
    private Bitboard _blackRooks;
    private Bitboard _blackQueens;
    private Bitboard _blackKing;

    public Bitboard WhitePawns => _whitePawns;
    public Bitboard WhiteKnights => _whiteKnights;
    public Bitboard WhiteBishops => _whiteBishops;
    public Bitboard WhiteRooks => _whiteRooks;
    public Bitboard WhiteQueens => _whiteQueens;
    public Bitboard WhiteKing => _whiteKing;

    public Bitboard BlackPawns => _blackPawns;
    public Bitboard BlackKnights => _blackKnights;
    public Bitboard BlackBishops => _blackBishops;
    public Bitboard BlackRooks => _blackRooks;
    public Bitboard BlackQueens => _blackQueens;
    public Bitboard BlackKing => _blackKing;

    public Bitboard WhitePieces { get; private set; }
    public Bitboard BlackPieces { get; private set; }
    public Bitboard AllPieces { get; private set; }

    public bool WhiteToMove { get; private set; }
    public CastlingRights CastlingRights { get; private set; }
    public Square EnPassantTarget { get; private set; } = Square.None;
    public int HalfmoveClock { get; private set; }
    public ulong ZobristHash { get; private set; }

    private readonly Stack<MoveHistory> _moveHistory = new(256);
    private readonly ulong[] _repetitionTable = new ulong[256];
    private int _currentPly;

    #endregion

    private Bitboard _whiteAttacks;
    private Bitboard _blackAttacks;
    public Bitboard WhiteAttacks => _whiteAttacks;
    public Bitboard BlackAttacks => _blackAttacks;

    /// <summary>
    /// Default constructor: sets up the standard starting position.
    /// </summary>
    public Position()
    {
        // Hard-coded bit masks for the initial position.
        _whitePawns   = new Bitboard(0xFF00UL);
        _whiteKnights = new Bitboard(0x42UL);
        _whiteBishops = new Bitboard(0x24UL);
        _whiteRooks   = new Bitboard(0x81UL);
        _whiteQueens  = new Bitboard(0x8UL);
        _whiteKing    = new Bitboard(0x10UL);

        _blackPawns   = new Bitboard(0xFF000000000000UL);
        _blackKnights = new Bitboard(0x4200000000000000UL);
        _blackBishops = new Bitboard(0x2400000000000000UL);
        _blackRooks   = new Bitboard(0x8100000000000000UL);
        _blackQueens  = new Bitboard(0x800000000000000UL);
        _blackKing    = new Bitboard(0x1000000000000000UL);

        CastlingRights = CastlingRights.BlackKingside | CastlingRights.BlackQueenside |
                         CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside;
        WhiteToMove = true;
        DeriveCombinedBitboards();
        ZobristHash = Zobrist.ComputeHash(this);
    }

    public Position(ReadOnlySpan<char> fen)
    {
        var enumerator = fen.Split(' ');
        enumerator.MoveNext();

        // Parse piece placement
        var piecePlacement = fen[enumerator.Current]; enumerator.MoveNext();
        var square = 56; // Start at a8
        for (var i = 0; i < piecePlacement.Length; i++)
        {
            var c = piecePlacement[i];
            if (char.IsDigit(c))
                square += c - '0'; // Empty squares
            else if (c == '/')
                square -= 16; // Move to next rank
            else
            {
                var pieceMask = Bitboard.Mask(square++);
                switch (c)
                {
                    case 'P':
                        _whitePawns.SetBits(pieceMask);
                        break;
                    case 'N':
                        _whiteKnights.SetBits(pieceMask);
                        break;
                    case 'B':
                        _whiteBishops.SetBits(pieceMask);
                        break;
                    case 'R':
                        _whiteRooks.SetBits(pieceMask);
                        break;
                    case 'Q':
                        _whiteQueens.SetBits(pieceMask);
                        break;
                    case 'K':
                        _whiteKing.SetBits(pieceMask);
                        break;
                    case 'p':
                        _blackPawns.SetBits(pieceMask);
                        break;
                    case 'n':
                        _blackKnights.SetBits(pieceMask);
                        break;
                    case 'b':
                        _blackBishops.SetBits(pieceMask);
                        break;
                    case 'r':
                        _blackRooks.SetBits(pieceMask);
                        break;
                    case 'q':
                        _blackQueens.SetBits(pieceMask);
                        break;
                    case 'k':
                        _blackKing.SetBits(pieceMask);
                        break;
                    default:
                        throw new ArgumentException($"Invalid FEN piece: {c}");
                }
            }
        }

        var activeColor = fen[enumerator.Current]; enumerator.MoveNext();
        WhiteToMove = activeColor[0] switch
        {
            'w' => true,
            'b' => false,
            _ => throw new ArgumentException("Invalid active color.")
        };

        // Parse castling rights
        var castlingRights = fen[enumerator.Current]; enumerator.MoveNext();

        if (castlingRights.IndexOf('K') != -1) CastlingRights |= CastlingRights.WhiteKingside;
        if (castlingRights.IndexOf('Q') != -1) CastlingRights |= CastlingRights.WhiteQueenside;
        if (castlingRights.IndexOf('k') != -1) CastlingRights |= CastlingRights.BlackKingside;
        if (castlingRights.IndexOf('q') != -1) CastlingRights |= CastlingRights.BlackQueenside;

        // Parse en passant square
        var enPassantSquare = fen[enumerator.Current]; enumerator.MoveNext();
        if (enPassantSquare is not "-")
        {
            EnPassantTarget = Enum.Parse<Square>(enPassantSquare);
        }
        else
        {
            EnPassantTarget = Square.None;
        }

        var halfmoveClock = fen[enumerator.Current]; enumerator.MoveNext();
        HalfmoveClock = int.Parse(halfmoveClock);

        // Update combined bitboards
        DeriveCombinedBitboards();
        _whiteAttacks = CalculateAttacks(true);
        _blackAttacks = CalculateAttacks(false);
        ZobristHash = Zobrist.ComputeHash(this);
        _repetitionTable[_currentPly++] = ZobristHash;
    }

    public void MakeMove(Move move)
    {
        var from = move.From;
        var to = move.To;
        var promotedPieceType = move.PromotedPieceType;
        var isCapture = move.IsCapture;
        var specialMoveType = move.SpecialMoveType;

        var fromMask = Bitboard.Mask(from);
        var toMask = Bitboard.Mask(to);

        var capturedPieceMask = DetermineCapturedPiece(move, from, to);

        // Save undo information
        SaveMoveUndo(move, fromMask, toMask, capturedPieceMask);

        // Get the moving piece's bitboard
        ref var movingPieceBoard = ref GetPieceBitboard(fromMask);

        switch (specialMoveType)
        {
            case SpecialMoveType.None:
                if (promotedPieceType != PromotedPieceType.None)
                {
                    HandlePromotionMove(move, toMask);
                    break;
                }

                HandleQuietOrCaptureMove(ref movingPieceBoard, toMask);
                break;
            case SpecialMoveType.DoublePawnPush:
                HandleQuietOrCaptureMove(ref movingPieceBoard, toMask);
                break;
            case SpecialMoveType.EnPassant:
                HandleEnPassantMove(fromMask, toMask);
                break;
            case SpecialMoveType.ShortCastle:
            case SpecialMoveType.LongCastle:
                HandleCastlingMove(to, ref movingPieceBoard);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Remove the moving piece from its original square
        movingPieceBoard.ClearBit(from);

        // Remove any captured piece and update castling rights if necessary
        if (isCapture)
        {
            HandleCapturedPiece(capturedPieceMask!.Value, move);
        }

        UpdateCastlingRightsAfterMove(from);

        // Set en passant target (if a double pawn push) or clear it.
        if (specialMoveType == SpecialMoveType.DoublePawnPush)
        {
            if (WhiteToMove)
            {
                EnPassantTarget = from + 8;
            }
            else
            {
                EnPassantTarget = from - 8;
            }
        }
        else
        {
            EnPassantTarget = Square.None;
        }

        // Update the halfmove clock
        var isPawnMove = (_whitePawns & fromMask).IsNotEmpty() || (_blackPawns & fromMask).IsNotEmpty();
        if (isPawnMove || isCapture)
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        DeriveCombinedBitboards();
        UpdateAttackBitboards();
        WhiteToMove = !WhiteToMove;
        ZobristHash = Zobrist.ComputeHash(this);
        _repetitionTable[_currentPly++] = ZobristHash;

    }

    private Bitboard? DetermineCapturedPiece(Move move, Square from, Square to)
    {
        if (!move.IsCapture)
        {
            return null;
        }

        var captured = AllPieces & Bitboard.Mask(to);
        if (move.SpecialMoveType != SpecialMoveType.EnPassant)
        {
            return captured;
        }

        // En Passant capture
        var capturedPawnSquare = to + (to > from ? -8 : 8);
        captured = Bitboard.Mask(capturedPawnSquare);
        return captured;
    }

    private void SaveMoveUndo(Move move, Bitboard fromMask, Bitboard toMask, Bitboard? capturedPieceMask)
    {
        var capturedType = PieceType.None;
        if (move.IsCapture)
        {
            capturedType = move.CapturedPieceType;
        }

        var movingPieceType = GetPieceTypeWithOverlap(fromMask);

        _moveHistory.Push(new MoveHistory
        {
            CapturedPiece = capturedPieceMask,
            FromSquare = fromMask,
            ToSquare = toMask,
            Move = move,
            CapturedPieceType = capturedType,
            MovedPieceType = movingPieceType,
            PreviousCastlingRights = CastlingRights,
            PreviousEnPassantTarget = EnPassantTarget,
            PreviousHalfmoveClock = HalfmoveClock,
            PreviousZobristHash = ZobristHash,
            PreviousWhiteAttacks = _whiteAttacks,
            PreviousBlackAttacks = _blackAttacks
        });
    }

    private void HandleQuietOrCaptureMove(ref Bitboard pieceBoard, Bitboard toMask)
    {
        pieceBoard.SetBits(toMask);
    }

    private void HandleCastlingMove(Square to, ref Bitboard kingBoard)
    {
        // Move the king
        kingBoard.SetBit(to);

        // Move the rook based on destination square
        switch (to)
        {
            case Square.c1: // White Queen-side castling
                _whiteRooks.ClearBit(Square.a1);
                _whiteRooks.SetBit(Square.d1);
                break;
            case Square.g1: // White King-side castling
                _whiteRooks.ClearBit(Square.h1);
                _whiteRooks.SetBit(Square.f1);
                break;
            case Square.c8: // Black Queen-side castling
                _blackRooks.ClearBit(Square.a8);
                _blackRooks.SetBit(Square.d8);
                break;
            case Square.g8: // Black King-side castling
                _blackRooks.ClearBit(Square.h8);
                _blackRooks.SetBit(Square.f8);
                break;
        }
    }

    private void HandleEnPassantMove(Bitboard fromMask, Bitboard toMask)
    {
        ref var pawnBoard = ref GetPieceBitboard(fromMask);
        pawnBoard.SetBits(toMask);
        // Removal of the captured pawn is handled by the captured piece logic.
    }

    private void HandlePromotionMove(Move move, Bitboard toMask)
    {
        switch (move.PromotedPieceType)
        {
            case PromotedPieceType.Knight:
                if (WhiteToMove) _whiteKnights.SetBits(toMask); else _blackKnights.SetBits(toMask);
                break;
            case PromotedPieceType.Bishop:
                if (WhiteToMove) _whiteBishops.SetBits(toMask); else _blackBishops.SetBits(toMask);
                break;
            case PromotedPieceType.Rook:
                if (WhiteToMove) _whiteRooks.SetBits(toMask); else _blackRooks.SetBits(toMask);
                break;
            case PromotedPieceType.Queen:
                if (WhiteToMove) _whiteQueens.SetBits(toMask); else _blackQueens.SetBits(toMask);
                break;
            default:
                throw new InvalidOperationException("Invalid promotion move type");
        }
    }

    private void HandleCapturedPiece(Bitboard capturedPieceMask, Move move)
    {
        // Update castling rights if a rook was captured
        if ((capturedPieceMask & _whiteRooks).IsNotEmpty())
        {
            if (capturedPieceMask.Intersects(Square.a1)) CastlingRights &= ~CastlingRights.WhiteQueenside;
            if (capturedPieceMask.Intersects(Square.h1)) CastlingRights &= ~CastlingRights.WhiteKingside;
        }
        else if ((capturedPieceMask & _blackRooks).IsNotEmpty())
        {
            if (capturedPieceMask.Intersects(Square.a8)) CastlingRights &= ~CastlingRights.BlackQueenside;
            if (capturedPieceMask.Intersects(Square.h8)) CastlingRights &= ~CastlingRights.BlackKingside;
        }

        ref var capturedBoard = ref GetPieceBitboard(move.CapturedPieceType);
        capturedBoard.ClearBits(capturedPieceMask);
    }

    private void UpdateCastlingRightsAfterMove(Square from)
    {
        switch (from)
        {
            case Square.e1:
                CastlingRights &= ~(CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside);
                break;
            case Square.e8:
                CastlingRights &= ~(CastlingRights.BlackKingside | CastlingRights.BlackQueenside);
                break;
            case Square.a1:
                CastlingRights &= ~CastlingRights.WhiteQueenside;
                break;
            case Square.h1:
                CastlingRights &= ~CastlingRights.WhiteKingside;
                break;
            case Square.a8:
                CastlingRights &= ~CastlingRights.BlackQueenside;
                break;
            case Square.h8:
                CastlingRights &= ~CastlingRights.BlackKingside;
                break;
        }
    }

    public void UndoMove()
    {
        if (_moveHistory.Count == 0)
            throw new InvalidOperationException("No move to unmake.");

        var lastUndo = _moveHistory.Pop();
        var move = lastUndo.Move;

        switch (move.SpecialMoveType)
        {
            case SpecialMoveType.None:
            case SpecialMoveType.DoublePawnPush:
                if (move.PromotedPieceType != PromotedPieceType.None)
                {
                    UndoPromotionMove(lastUndo);
                    break;
                }

                UndoQuietOrCaptureMove(lastUndo);
                break;
            case SpecialMoveType.EnPassant:
                UndoEnPassantMove(lastUndo);
                break;
            case SpecialMoveType.ShortCastle:
            case SpecialMoveType.LongCastle:
                UndoCastlingMove(lastUndo);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Restore captured piece if any
        if (lastUndo.CapturedPieceType != PieceType.None && move.SpecialMoveType != SpecialMoveType.EnPassant)
        {
            ref var capturedBoard = ref GetPieceBitboard(lastUndo.CapturedPieceType);
            capturedBoard.SetBits(lastUndo.CapturedPiece!.Value);
        }

        EnPassantTarget = lastUndo.PreviousEnPassantTarget;
        CastlingRights = lastUndo.PreviousCastlingRights;
        HalfmoveClock = lastUndo.PreviousHalfmoveClock;

        DeriveCombinedBitboards();
        WhiteToMove = !WhiteToMove;
        if (_currentPly > 0) _currentPly--;
        ZobristHash = lastUndo.PreviousZobristHash;
        _whiteAttacks = lastUndo.PreviousWhiteAttacks;
        _blackAttacks = lastUndo.PreviousBlackAttacks;
    }

    private void UndoQuietOrCaptureMove(MoveHistory lastHistory)
    {
        ref var movedPieceBoard = ref GetPieceBitboard(lastHistory.MovedPieceType);
        movedPieceBoard.ClearBits(lastHistory.ToSquare);
        movedPieceBoard.SetBits(lastHistory.FromSquare);
    }

    private void UndoCastlingMove(MoveHistory lastHistory)
    {
        UndoQuietOrCaptureMove(lastHistory);

        switch (lastHistory.Move.To)
        {
            case Square.c1:
                _whiteRooks.ClearBit(Square.d1);
                _whiteRooks.SetBit(Square.a1);
                break;
            case Square.g1:
                _whiteRooks.ClearBit(Square.f1);
                _whiteRooks.SetBit(Square.h1);
                break;
            case Square.c8:
                _blackRooks.ClearBit(Square.d8);
                _blackRooks.SetBit(Square.a8);
                break;
            case Square.g8:
                _blackRooks.ClearBit(Square.f8);
                _blackRooks.SetBit(Square.h8);
                break;
        }
    }

    private void UndoEnPassantMove(MoveHistory lastHistory)
    {
        // First, undo the moving pawn's relocation.
        UndoQuietOrCaptureMove(lastHistory);

        // Calculate the square of the captured pawn.
        var capturedPawnSquare = lastHistory.Move.To + (lastHistory.Move.To > lastHistory.Move.From ? -8 : 8);
        var capturedPawnMask = Bitboard.Mask(capturedPawnSquare);

        // Use the captured pawn type from the undo record.
        ref var pawnBoard = ref GetPieceBitboard(lastHistory.CapturedPieceType);
        pawnBoard.SetBits(capturedPawnMask);
    }

    private void UndoPromotionMove(MoveHistory lastHistory)
    {
        var isWhite = !WhiteToMove;
        if (isWhite)
        {
            ref var pawnBoard = ref GetPieceBitboard(PieceType.WhitePawn);
            pawnBoard.SetBits(lastHistory.FromSquare);
        }
        else
        {
            ref var pawnBoard = ref GetPieceBitboard(PieceType.BlackPawn);
            pawnBoard.SetBits(lastHistory.FromSquare);
        }

        switch (lastHistory.Move.PromotedPieceType)
        {
            case PromotedPieceType.Knight:
                if (isWhite)
                    _whiteKnights.ClearBits(lastHistory.ToSquare);
                else
                    _blackKnights.ClearBits(lastHistory.ToSquare);
                break;
            case PromotedPieceType.Bishop:
                if (isWhite)
                    _whiteBishops.ClearBits(lastHistory.ToSquare);
                else
                    _blackBishops.ClearBits(lastHistory.ToSquare);
                break;
            case PromotedPieceType.Rook:
                if (isWhite)
                    _whiteRooks.ClearBits(lastHistory.ToSquare);
                else
                    _blackRooks.ClearBits(lastHistory.ToSquare);
                break;
            case PromotedPieceType.Queen:
                if (isWhite)
                    _whiteQueens.ClearBits(lastHistory.ToSquare);
                else
                    _blackQueens.ClearBits(lastHistory.ToSquare);
                break;
        }
    }

    private PieceType GetPieceTypeWithOverlap(Bitboard capturedPieceMask)
    {
        if ((_whitePawns & capturedPieceMask).IsNotEmpty()) return PieceType.WhitePawn;
        if ((_whiteKnights & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteKnight;
        if ((_whiteBishops & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteBishop;
        if ((_whiteRooks  & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteRook;
        if ((_whiteQueens & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteQueen;
        if ((_whiteKing & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteKing;
        if ((_blackPawns &  capturedPieceMask).IsNotEmpty()) return PieceType.BlackPawn;
        if ((_blackKnights & capturedPieceMask).IsNotEmpty()) return PieceType.BlackKnight;
        if ((_blackBishops & capturedPieceMask).IsNotEmpty()) return PieceType.BlackBishop;
        if ((_blackRooks  & capturedPieceMask).IsNotEmpty()) return PieceType.BlackRook;
        if ((_blackQueens & capturedPieceMask).IsNotEmpty()) return PieceType.BlackQueen;
        if ((_blackKing & capturedPieceMask).IsNotEmpty()) return PieceType.BlackKing;
        return PieceType.None;
    }

    private ref Bitboard GetPieceBitboard(Bitboard bitboard)
    {
        if ((_whitePawns & bitboard).IsNotEmpty()) return ref _whitePawns;
        if ((_whiteKnights & bitboard).IsNotEmpty()) return ref _whiteKnights;
        if ((_whiteBishops & bitboard).IsNotEmpty()) return ref _whiteBishops;
        if ((_whiteRooks & bitboard).IsNotEmpty()) return ref _whiteRooks;
        if ((_whiteQueens & bitboard).IsNotEmpty()) return ref _whiteQueens;
        if ((_whiteKing & bitboard).IsNotEmpty()) return ref _whiteKing;

        if ((_blackPawns & bitboard).IsNotEmpty()) return ref _blackPawns;
        if ((_blackKnights & bitboard).IsNotEmpty()) return ref _blackKnights;
        if ((_blackBishops & bitboard).IsNotEmpty()) return ref _blackBishops;
        if ((_blackRooks & bitboard).IsNotEmpty()) return ref _blackRooks;
        if ((_blackQueens & bitboard).IsNotEmpty()) return ref _blackQueens;
        if ((_blackKing & bitboard).IsNotEmpty()) return ref _blackKing;

        throw new InvalidOperationException("No matching piece found for given bitboard.");
    }

    private ref Bitboard GetPieceBitboard(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.WhitePawn: return ref _whitePawns;
            case PieceType.WhiteKnight: return ref _whiteKnights;
            case PieceType.WhiteBishop: return ref _whiteBishops;
            case PieceType.WhiteRook: return ref _whiteRooks;
            case PieceType.WhiteQueen: return ref _whiteQueens;
            case PieceType.WhiteKing: return ref _whiteKing;
            case PieceType.BlackPawn: return ref _blackPawns;
            case PieceType.BlackKnight: return ref _blackKnights;
            case PieceType.BlackBishop: return ref _blackBishops;
            case PieceType.BlackRook: return ref _blackRooks;
            case PieceType.BlackQueen: return ref _blackQueens;
            case PieceType.BlackKing: return ref _blackKing;
            case PieceType.None:
            default: throw new InvalidOperationException("No matching piece found for given piece type.");
        }
    }

    private void DeriveCombinedBitboards()
    {
        WhitePieces = _whitePawns | _whiteKnights | _whiteBishops | _whiteRooks | _whiteQueens | _whiteKing;
        BlackPieces = _blackPawns | _blackKnights | _blackBishops | _blackRooks | _blackQueens | _blackKing;
        AllPieces = WhitePieces | BlackPieces;
    }

    public bool IsInCheck()
    {
        return IsInCheck(WhiteToMove);
    }

    public bool IsInCheck(bool isWhite)
    {
        var kingSquare = isWhite ? (Square)BitOperations.TrailingZeroCount(_whiteKing) : (Square)BitOperations.TrailingZeroCount(_blackKing);
        return IsSquareAttacked(kingSquare, byWhite: !isWhite);
    }

    public bool IsSquareAttacked(Square square, bool byWhite)
    {
        var enemyAttacks = byWhite ? _whiteAttacks : _blackAttacks;
        return (square.ToMask() & enemyAttacks) != 0;
    }

    private Bitboard CalculateAttacks(bool forWhite)
    {
        Bitboard attacks = 0;

        // Calculate pawn attacks
        var pawns = forWhite ? _whitePawns : _blackPawns;
        attacks |= CalculatePawnAttacks(pawns, forWhite);

        // Calculate knight attacks
        var knights = forWhite ? _whiteKnights : _blackKnights;
        attacks |= CalculateKnightAttacks(knights);

        // Calculate bishop/queen diagonal attacks
        var bishopsQueens = forWhite ?
            _whiteBishops | _whiteQueens :
            _blackBishops | _blackQueens;
        attacks |= CalculateDiagonalAttacks(bishopsQueens);

        // Calculate rook/queen straight attacks
        var rooksQueens = forWhite ?
            _whiteRooks | _whiteQueens :
            _blackRooks | _blackQueens;
        attacks |= CalculateOrthogonalAttacks(rooksQueens);

        // Calculate king attacks
        var king = forWhite ? _whiteKing : _blackKing;
        attacks |= CalculateKingAttacks(king);

        return attacks;
    }

    private Bitboard CalculatePawnAttacks(Bitboard pawns, bool isWhite)
    {
        Bitboard attacks;
        if (isWhite)
        {
            var upLeftAttacks = (pawns << 7) & ~Constants.FileH; // Up-left: shift northeast and mask off H-file
            var upRightAttacks = (pawns << 9) & ~Constants.FileA; // Up-right: shift northwest and mask off A-file
            attacks = upLeftAttacks | upRightAttacks;
        }
        else
        {
            var downLeftAttacks = (pawns >> 9) & ~Constants.FileH; // Down-left: shift southeast and mask off h-file
            var downRightAttacks = (pawns >> 7) & ~Constants.FileA; // Down-right: shift southwest and mask off a-file
            attacks = downLeftAttacks | downRightAttacks;
        }

        return attacks;
    }

    private Bitboard CalculateKnightAttacks(Bitboard knights)
    {
        Bitboard attacks = 0;

        while (knights != 0)
        {
            var knightSquare = BitOperations.TrailingZeroCount(knights);
            var knightFile = knightSquare % 8;
            var knightRank = knightSquare / 8;

            // Knight's 8 possible moves
            Span<int> fileOffsets = [-2, -2, -1, -1, 1, 1, 2, 2];
            Span<int> rankOffsets = [-1, 1, -2, 2, -2, 2, -1, 1];

            for (var i = 0; i < 8; i++)
            {
                var targetFile = knightFile + fileOffsets[i];
                var targetRank = knightRank + rankOffsets[i];

                // Check if target square is on the board
                if (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
                {
                    var targetSquare = targetRank * 8 + targetFile;
                    attacks |= Bitboard.Mask(targetSquare);
                }
            }

            knights &= knights - 1;
        }

        return attacks;
    }

    private Bitboard CalculateDiagonalAttacks(Bitboard diagonalRayAttackers)
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

                    if ((AllPieces & squareMask) != 0)
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

    private Bitboard CalculateOrthogonalAttacks(Bitboard orthogonalRayAttackers)
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

                    if ((AllPieces & squareMask) != 0)
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

    private Bitboard CalculateKingAttacks(Bitboard king)
    {
        if (king == 0) return 0;

        var kingSquare = BitOperations.TrailingZeroCount(king);
        var kingFile = kingSquare % 8;
        var kingRank = kingSquare / 8;

        Bitboard attacks = 0;

        // King attacks all 8 surrounding squares (-1, 0, 1 for both file and rank)
        for (var fileOffset = -1; fileOffset <= 1; fileOffset++)
        {
            for (var rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                // Skip the king's own square
                if (fileOffset == 0 && rankOffset == 0) continue;

                var targetFile = kingFile + fileOffset;
                var targetRank = kingRank + rankOffset;

                if (targetFile is >= 0 and < 8 && targetRank is >= 0 and < 8)
                {
                    var targetSquare = targetRank * 8 + targetFile;
                    attacks |= Bitboard.Mask(targetSquare);
                }
            }
        }

        return attacks;
    }

    private void UpdateAttackBitboards()
    {
        _whiteAttacks = CalculateAttacks(true);
        _blackAttacks = CalculateAttacks(false);
    }

    public bool IsDrawByRepetition()
    {
        var count = 0;
        for (var i = 0; i < _currentPly; i++)
        {
            if (_repetitionTable[i] != ZobristHash) continue;

            count++;
            if (count >= 3)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDrawByInsufficientMaterial()
    {
        var whiteMaterial = _whitePawns | _whiteKnights | _whiteBishops | _whiteRooks | _whiteQueens;
        var blackMaterial = _blackPawns | _blackKnights | _blackBishops | _blackRooks | _blackQueens;

        // Both sides have just kings
        if (whiteMaterial.IsEmpty() && blackMaterial.IsEmpty())
        {
            return true;
        }

        // If white has only its king
        if (whiteMaterial.IsEmpty())
        {
            // Black has exactly one knight
            if (blackMaterial == _blackKnights && blackMaterial.Count() == 1)
                return true;
            // or exactly one bishop
            if (blackMaterial == _blackBishops && blackMaterial.Count() == 1)
                return true;
        }

        // If black has only its king
        if (blackMaterial.IsEmpty())
        {
            // White has exactly one knight
            if (whiteMaterial == _whiteKnights && whiteMaterial.Count() == 1)
                return true;
            // or exactly one bishop
            if (whiteMaterial == _whiteBishops && whiteMaterial.Count() == 1)
                return true;
        }

        return false;
    }

    public PieceType GetPieceTypeAt(Square square, bool isWhite)
    {
        var mask = Bitboard.Mask(square);
        if (isWhite)
        {
            if ((_whitePawns & mask).IsNotEmpty()) return PieceType.WhitePawn;
            if ((_whiteKnights & mask).IsNotEmpty()) return PieceType.WhiteKnight;
            if ((_whiteBishops & mask).IsNotEmpty()) return PieceType.WhiteBishop;
            if ((_whiteRooks & mask).IsNotEmpty()) return PieceType.WhiteRook;
            if ((_whiteQueens & mask).IsNotEmpty()) return PieceType.WhiteQueen;
            if ((_whiteKing & mask).IsNotEmpty()) return PieceType.WhiteKing;
        }
        else
        {
            if ((_blackPawns & mask).IsNotEmpty()) return PieceType.BlackPawn;
            if ((_blackKnights & mask).IsNotEmpty()) return PieceType.BlackKnight;
            if ((_blackBishops & mask).IsNotEmpty()) return PieceType.BlackBishop;
            if ((_blackRooks & mask).IsNotEmpty()) return PieceType.BlackRook;
            if ((_blackQueens & mask).IsNotEmpty()) return PieceType.BlackQueen;
            if ((_blackKing & mask).IsNotEmpty()) return PieceType.BlackKing;
        }

        return PieceType.None;
    }
}
