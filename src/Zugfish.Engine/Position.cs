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
        MemoryExtensions.SpanSplitEnumerator<char> enumerator = fen.Split(' ');
        enumerator.MoveNext();

        // Parse piece placement
        var piecePlacement = fen[enumerator.Current]; enumerator.MoveNext();
        var square = 56; // Start at a8
        foreach (var c in piecePlacement)
        {
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
        ZobristHash = Zobrist.ComputeHash(this);
        _repetitionTable[_currentPly++] = ZobristHash;
    }

    public void MakeMove(Move move)
    {
        var from = move.From;
        var to = move.To;
        var fromMask = Bitboard.Mask(from);
        var toMask = Bitboard.Mask(to);

        // Determine captured piece
        var capturedPieceMask = DetermineCapturedPiece(move.Type, from, to, toMask);
        Bitboard dummyCapturedPiece = default;
        ref var capturedPieceBoard = ref capturedPieceMask.IsNotEmpty()
            ? ref GetPieceBitboard(capturedPieceMask)
            : ref dummyCapturedPiece;

        // Save undo information
        SaveMoveUndo(move, fromMask, toMask, capturedPieceMask);

        // Get the moving piece's bitboard
        ref var movingPieceBoard = ref GetPieceBitboard(fromMask);

        switch (move.Type)
        {
            case MoveType.Quiet:
            case MoveType.Capture:
            case MoveType.DoublePawnPush:
                HandleQuietOrCaptureMove(ref movingPieceBoard, toMask);
                break;
            case MoveType.Castling:
                HandleCastlingMove(move.To, ref movingPieceBoard);
                break;
            case MoveType.EnPassant:
                HandleEnPassantMove(fromMask, toMask);
                break;
            case MoveType.PromoteToKnight:
            case MoveType.PromoteToBishop:
            case MoveType.PromoteToRook:
            case MoveType.PromoteToQueen:
                HandlePromotionMove(move, to, toMask);
                break;
            default:
                throw new InvalidOperationException("Unhandled move type");
        }

        // Remove the moving piece from its original square
        movingPieceBoard.ClearBit(from);

        // Remove any captured piece and update castling rights if necessary
        if (capturedPieceMask.IsNotEmpty())
        {
            HandleCapturedPiece(capturedPieceMask, ref capturedPieceBoard);
        }

        UpdateCastlingRightsAfterMove(from);

        // Set en passant target (if a double pawn push) or clear it.
        if (move.Type == MoveType.DoublePawnPush)
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
        if (isPawnMove || capturedPieceMask.IsNotEmpty())
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        DeriveCombinedBitboards();
        WhiteToMove = !WhiteToMove;
        ZobristHash = Zobrist.ComputeHash(this);
        _repetitionTable[_currentPly++] = ZobristHash;

    }

    private Bitboard DetermineCapturedPiece(MoveType moveType, Square from, Square to, Bitboard toMask)
    {
        var captured = AllPieces & toMask;
        if (moveType != MoveType.EnPassant)
        {
            return captured;
        }

        // En Passant capture
        var capturedPawnSquare = to + (to > from ? -8 : 8);
        captured = Bitboard.Mask(capturedPawnSquare);
        return captured;
    }

    private void SaveMoveUndo(Move move, Bitboard fromMask, Bitboard toMask, Bitboard capturedPieceMask)
    {
        var capturedType = PieceType.None;
        if (capturedPieceMask.IsNotEmpty())
        {
            capturedType = GetPieceTypeWithOverlap(capturedPieceMask);
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
            PreviousZobristHash = ZobristHash
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

    private void HandlePromotionMove(Move move, Square to, Bitboard toMask)
    {
        switch (move.Type)
        {
            case MoveType.PromoteToKnight:
                if (WhiteToMove) _whiteKnights.SetBits(toMask); else _blackKnights.SetBits(toMask);
                break;
            case MoveType.PromoteToBishop:
                if (WhiteToMove) _whiteBishops.SetBits(toMask); else _blackBishops.SetBits(toMask);
                break;
            case MoveType.PromoteToRook:
                if (WhiteToMove) _whiteRooks.SetBits(toMask); else _blackRooks.SetBits(toMask);
                break;
            case MoveType.PromoteToQueen:
                if (WhiteToMove) _whiteQueens.SetBits(toMask); else _blackQueens.SetBits(toMask);
                break;
            default:
                throw new InvalidOperationException("Invalid promotion move type");
        }
    }

    private void HandleCapturedPiece(Bitboard capturedPieceMask, ref Bitboard capturedBoard)
    {
        capturedBoard.ClearBits(capturedPieceMask);

        // Update castling rights if a rook was captured
        if ((capturedPieceMask & _whiteRooks).IsNotEmpty())
        {
            if ((capturedPieceMask & (1UL << 0)).IsNotEmpty()) CastlingRights &= CastlingRights.WhiteKingside;
            if ((capturedPieceMask & (1UL << 7)).IsNotEmpty()) CastlingRights &= CastlingRights.WhiteQueenside;
        }
        else if ((capturedPieceMask & _blackRooks).IsNotEmpty())
        {
            if ((capturedPieceMask & (1UL << 56)).IsNotEmpty()) CastlingRights &= CastlingRights.BlackKingside;
            if ((capturedPieceMask & (1UL << 63)).IsNotEmpty()) CastlingRights &= CastlingRights.BlackQueenside;
        }
    }

    private void UpdateCastlingRightsAfterMove(Square from)
    {
        switch (from)
        {
            case Square.e1:
                CastlingRights &= CastlingRights.BlackKingside | CastlingRights.BlackQueenside;
                break;
            case Square.e8:
                CastlingRights &= CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside;
                break;
            case Square.a1:
                CastlingRights &= CastlingRights.WhiteQueenside;
                break;
            case Square.h1:
                CastlingRights &= CastlingRights.WhiteKingside;
                break;
            case Square.a8:
                CastlingRights &= CastlingRights.BlackQueenside;
                break;
            case Square.h8:
                CastlingRights &= CastlingRights.BlackKingside;
                break;
        }
    }

    public void UndoMove()
    {
        if (_moveHistory.Count == 0)
            throw new InvalidOperationException("No move to unmake.");

        var lastUndo = _moveHistory.Pop();
        var move = lastUndo.Move;

        // Dispatch undo handling based on move type
        switch (move.Type)
        {
            case MoveType.Quiet:
            case MoveType.Capture:
            case MoveType.DoublePawnPush:
                UndoQuietOrCaptureMove(lastUndo);
                break;
            case MoveType.Castling:
                UndoCastlingMove(lastUndo);
                break;
            case MoveType.EnPassant:
                UndoEnPassantMove(lastUndo);
                break;
            case MoveType.PromoteToKnight:
            case MoveType.PromoteToBishop:
            case MoveType.PromoteToRook:
            case MoveType.PromoteToQueen:
                UndoPromotionMove(lastUndo);
                break;
            default:
                throw new InvalidOperationException("Unhandled move type during undo");
        }

        // Restore captured piece if any
        if (lastUndo.CapturedPiece.IsNotEmpty() && move.Type != MoveType.EnPassant)
        {
            ref var capturedBoard = ref GetPieceBitboard(lastUndo.CapturedPieceType);
            capturedBoard.SetBits(lastUndo.CapturedPiece);
        }

        EnPassantTarget = lastUndo.PreviousEnPassantTarget;
        CastlingRights = lastUndo.PreviousCastlingRights;
        HalfmoveClock = lastUndo.PreviousHalfmoveClock;

        DeriveCombinedBitboards();
        WhiteToMove = !WhiteToMove;
        if (_currentPly > 0) _currentPly--;
        ZobristHash = lastUndo.PreviousZobristHash;
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

        switch (lastHistory.Move.Type)
        {
            case MoveType.PromoteToKnight:
                if (isWhite)
                    _whiteKnights.ClearBits(lastHistory.ToSquare);
                else
                    _blackKnights.ClearBits(lastHistory.ToSquare);
                break;
            case MoveType.PromoteToBishop:
                if (isWhite)
                    _whiteBishops.ClearBits(lastHistory.ToSquare);
                else
                    _blackBishops.ClearBits(lastHistory.ToSquare);
                break;
            case MoveType.PromoteToRook:
                if (isWhite)
                    _whiteRooks.ClearBits(lastHistory.ToSquare);
                else
                    _blackRooks.ClearBits(lastHistory.ToSquare);
                break;
            case MoveType.PromoteToQueen:
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
        var kingBoard = WhiteToMove ? _whiteKing : _blackKing;
        return IsSquareAttacked(BitOperations.TrailingZeroCount(kingBoard), WhiteToMove);
    }

    public bool IsSquareAttacked(int square, bool byWhite)
    {
        var file = square % 8;
        var rank = square / 8;

        // Pawn attacks
        if (byWhite)
        {
            if (file > 0)
            {
                var pawnSquare = square - 9;
                if (pawnSquare >= 0 && (_whitePawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }

            if (file < 7)
            {
                var pawnSquare = square - 7;
                if (pawnSquare >= 0 && (_whitePawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }
        }
        else
        {
            if (file < 7)
            {
                var pawnSquare = square + 9;
                if (pawnSquare < 64 && (_blackPawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }

            if (file > 0)
            {
                var pawnSquare = square + 7;
                if (pawnSquare < 64 && (_blackPawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }
        }

        // Knight attacks
        Span<int> knightOffsets = [-17, -15, -10, -6, 6, 10, 15, 17];
        var knights = byWhite ? _whiteKnights : _blackKnights;
        for (var i = 0; i < knightOffsets.Length; i++)
        {
            var offset = knightOffsets[i];
            var attackerSquare = square + offset;
            if (attackerSquare is < 0 or >= 64) continue;

            var attackerFile = attackerSquare % 8;
            if (Math.Abs(attackerFile - file) > 2) continue;

            if ((knights & (1UL << attackerSquare)).IsNotEmpty()) return true;
        }

        // King attacks
        Span<int> kingOffsets = [-9, -8, -7, -1, 1, 7, 8, 9];
        var king = byWhite ? _whiteKing : _blackKing;
        for (var i = 0; i < kingOffsets.Length; i++)
        {
            var offset = kingOffsets[i];
            var attackerSquare = square + offset;
            if (attackerSquare is < 0 or >= 64) continue;

            var attackerFile = attackerSquare % 8;
            if (Math.Abs(attackerFile - file) > 1) continue;

            if ((king & (1UL << attackerSquare)).IsNotEmpty()) return true;
        }

        // Sliding piece attacks
        Span<(int, int)> diagonalDirections = [(1, 1), (-1, 1), (1, -1), (-1, -1)];
        for (var i = 0; i < diagonalDirections.Length; i++)
        {
            var (fileDirection, rankDirection) = diagonalDirections[i];
            var currentFile = file;
            var currentRank = rank;
            while (true)
            {
                currentFile += fileDirection;
                currentRank += rankDirection;
                if (currentFile < 0 || currentFile >= 8 || currentRank < 0 || currentRank >= 8) break;
                var toMask = Bitboard.Mask(currentRank * 8 + currentFile);

                if ((AllPieces & toMask).IsEmpty()) continue;

                // If a piece is blocking further movement, check if it's an attacker.
                if (byWhite)
                {
                    if (((_whiteBishops | _whiteQueens) & toMask).IsNotEmpty()) return true;
                }
                else
                {
                    if (((_blackBishops | _blackQueens) & toMask).IsNotEmpty()) return true;
                }

                break;
            }
        }

        Span<(int, int)> straightDirections = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        for (var i = 0; i < straightDirections.Length; i++)
        {
            var (fileDirection, rankDirection) = straightDirections[i];
            var currentFile = file;
            var currentRank = rank;
            while (true)
            {
                currentFile += fileDirection;
                currentRank += rankDirection;
                if (currentFile < 0 || currentFile >= 8 || currentRank < 0 || currentRank >= 8) break;
                var toMask = Bitboard.Mask(currentRank * 8 + currentFile);
                if ((AllPieces & toMask).IsEmpty()) continue;

                // If a piece is blocking further movement, check if it's an attacker.
                if (byWhite)
                {
                    if (((_whiteRooks | _whiteQueens) & toMask).IsNotEmpty()) return true;
                }
                else
                {
                    if (((_blackRooks | _blackQueens) & toMask).IsNotEmpty()) return true;
                }

                break;
            }
        }

        return false;
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
}
