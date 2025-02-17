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

    public Bitboard WhitePawns { get => _whitePawns; private set => _whitePawns = value; }
    public Bitboard WhiteKnights { get => _whiteKnights; private set => _whiteKnights = value; }
    public Bitboard WhiteBishops { get => _whiteBishops; private set => _whiteBishops = value; }
    public Bitboard WhiteRooks { get => _whiteRooks; private set => _whiteRooks = value; }
    public Bitboard WhiteQueens { get => _whiteQueens; private set => _whiteQueens = value; }
    public Bitboard WhiteKing { get => _whiteKing; private set => _whiteKing = value; }

    public Bitboard BlackPawns { get => _blackPawns; private set => _blackPawns = value; }
    public Bitboard BlackKnights { get => _blackKnights; private set => _blackKnights = value; }
    public Bitboard BlackBishops { get => _blackBishops; private set => _blackBishops = value; }
    public Bitboard BlackRooks { get => _blackRooks; private set => _blackRooks = value; }
    public Bitboard BlackQueens { get => _blackQueens; private set => _blackQueens = value; }
    public Bitboard BlackKing { get => _blackKing; private set => _blackKing = value; }

    public Bitboard WhitePieces { get; private set; }
    public Bitboard BlackPieces { get; private set; }

    private Bitboard _allPieces;
    public Bitboard AllPieces { get => _allPieces; private set => _allPieces = value; }

    public bool WhiteToMove { get; private set; }
    public ushort CastlingRights { get; private set; }
    public int EnPassantTarget { get; private set; } = -1;
    public int HalfmoveClock { get; private set; }
    public ulong ZobristHash { get; private set; }

    // TODO: Move these out of this class?
    private readonly Stack<MoveHistory> _moveHistory = new(256);
    // private readonly Dictionary<ulong, int> _repetitionTable = new(128);
    private readonly ulong[] _repetitionTable = new ulong[256];
    private int _currentPly = 0;

    #endregion

    /// <summary>
    /// Default constructor: sets up the standard starting position.
    /// </summary>
    public Position()
    {
        // Hard-coded bit masks for the initial position.
        WhitePawns   = new Bitboard(0xFF00UL);
        WhiteKnights = new Bitboard(0x42UL);
        WhiteBishops = new Bitboard(0x24UL);
        WhiteRooks   = new Bitboard(0x81UL);
        WhiteQueens  = new Bitboard(0x8UL);
        WhiteKing    = new Bitboard(0x10UL);

        BlackPawns   = new Bitboard(0xFF000000000000UL);
        BlackKnights = new Bitboard(0x4200000000000000UL);
        BlackBishops = new Bitboard(0x2400000000000000UL);
        BlackRooks   = new Bitboard(0x8100000000000000UL);
        BlackQueens  = new Bitboard(0x800000000000000UL);
        BlackKing    = new Bitboard(0x1000000000000000UL);

        CastlingRights = 0b1111;
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
                        _whitePawns |= pieceMask;
                        break;
                    case 'N':
                        _whiteKnights |= pieceMask;
                        break;
                    case 'B':
                        _whiteBishops |= pieceMask;
                        break;
                    case 'R':
                        _whiteRooks |= pieceMask;
                        break;
                    case 'Q':
                        _whiteQueens |= pieceMask;
                        break;
                    case 'K':
                        _whiteKing |= pieceMask;
                        break;
                    case 'p':
                        _blackPawns |= pieceMask;
                        break;
                    case 'n':
                        _blackKnights |= pieceMask;
                        break;
                    case 'b':
                        _blackBishops |= pieceMask;
                        break;
                    case 'r':
                        _blackRooks |= pieceMask;
                        break;
                    case 'q':
                        _blackQueens |= pieceMask;
                        break;
                    case 'k':
                        _blackKing |= pieceMask;
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

        if (castlingRights.IndexOf('K') != -1) CastlingRights |= 0b0001;
        if (castlingRights.IndexOf('Q') != -1) CastlingRights |= 0b0010;
        if (castlingRights.IndexOf('k') != -1) CastlingRights |= 0b0100;
        if (castlingRights.IndexOf('q') != -1) CastlingRights |= 0b1000;

        // Parse en passant square
        var enPassantSquare = fen[enumerator.Current]; enumerator.MoveNext();
        if (enPassantSquare is not "-")
        {
            var file = enPassantSquare[0];
            var rank = enPassantSquare[1];
            EnPassantTarget = 8 * (rank - '1') + (file - 'a');
        }
        else
        {
            EnPassantTarget = -1;
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
        movingPieceBoard = movingPieceBoard.Clear(from);

        // Remove any captured piece and update castling rights if necessary
        if (capturedPieceMask.IsNotEmpty())
        {
            HandleCapturedPiece(capturedPieceMask, ref capturedPieceBoard);
        }

        UpdateCastlingRightsAfterMove(from);

        // Set en passant target (if a double pawn push) or clear it.
        EnPassantTarget = move.Type == MoveType.DoublePawnPush ? (from + to) / 2 : -1;

        // Update the halfmove clock
        var isPawnMove = (WhitePawns & fromMask).IsNotEmpty() || (BlackPawns & fromMask).IsNotEmpty();
        if (isPawnMove || capturedPieceMask.IsNotEmpty())
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        DeriveCombinedBitboards();
        WhiteToMove = !WhiteToMove;
        ZobristHash = Zobrist.ComputeHash(this);
        _repetitionTable[_currentPly++] = ZobristHash;

    }

    private Bitboard DetermineCapturedPiece(MoveType moveType, int from, int to, Bitboard toMask)
    {
        var captured = _allPieces & toMask;
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
        pieceBoard = pieceBoard.Set(toMask);
    }

    private void HandleCastlingMove(int to, ref Bitboard kingBoard)
    {
        // Move the king
        kingBoard = kingBoard.Set(to);

        // Move the rook based on destination square
        switch (to)
        {
            case 2: // White Queen-side castling
                WhiteRooks = WhiteRooks.Clear(0);
                WhiteRooks = WhiteRooks.Set(3);
                break;
            case 6: // White King-side castling
                WhiteRooks = WhiteRooks.Clear(7);
                WhiteRooks = WhiteRooks.Set(5);
                break;
            case 58: // Black Queen-side castling
                BlackRooks = WhiteRooks.Clear(56);
                BlackRooks = WhiteRooks.Set(59);
                break;
            case 62: // Black King-side castling
                BlackRooks = WhiteRooks.Clear(59);
                BlackRooks = WhiteRooks.Set(61);
                break;
        }
    }

    private void HandleEnPassantMove(Bitboard fromMask, Bitboard toMask)
    {
        ref var pawnBoard = ref GetPieceBitboard(fromMask);
        pawnBoard = pawnBoard.Set(toMask);
        // Removal of the captured pawn is handled by the captured piece logic.
    }

    private void HandlePromotionMove(Move move, int to, Bitboard toMask)
    {
        var isWhite = to > 55; // Adjust based on your board representation
        switch (move.Type)
        {
            case MoveType.PromoteToKnight:
                if (isWhite) WhiteKnights = WhiteKnights.Set(toMask); else BlackKnights = BlackKnights.Set(toMask);
                break;
            case MoveType.PromoteToBishop:
                if (isWhite) WhiteBishops = WhiteBishops.Set(toMask); else BlackBishops = BlackBishops.Set(toMask);
                break;
            case MoveType.PromoteToRook:
                if (isWhite) WhiteRooks |= toMask; else BlackRooks |= toMask;
                break;
            case MoveType.PromoteToQueen:
                if (isWhite) WhiteQueens |= toMask; else BlackQueens |= toMask;
                break;
            default:
                throw new InvalidOperationException("Invalid promotion move type");
        }
    }

    private void HandleCapturedPiece(Bitboard capturedPieceMask, ref Bitboard capturedBoard)
    {
        capturedBoard &= ~capturedPieceMask;

        // Update castling rights if a rook was captured
        if ((capturedPieceMask & WhiteRooks).IsNotEmpty())
        {
            if ((capturedPieceMask & (1UL << 0)).IsNotEmpty()) CastlingRights &= 0b1101;
            if ((capturedPieceMask & (1UL << 7)).IsNotEmpty()) CastlingRights &= 0b1110;
        }
        else if ((capturedPieceMask & BlackRooks).IsNotEmpty())
        {
            if ((capturedPieceMask & (1UL << 56)).IsNotEmpty()) CastlingRights &= 0b0111;
            if ((capturedPieceMask & (1UL << 63)).IsNotEmpty()) CastlingRights &= 0b1011;
        }
    }

    private void UpdateCastlingRightsAfterMove(int from)
    {
        switch (from)
        {
            case 4:
                CastlingRights &= 0b1100;
                break;
            case 60:
                CastlingRights &= 0b0011;
                break;
            case 0:
                CastlingRights &= 0b1101;
                break;
            case 7:
                CastlingRights &= 0b1110;
                break;
            case 56:
                CastlingRights &= 0b0111;
                break;
            case 63:
                CastlingRights &= 0b1011;
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
            capturedBoard |= lastUndo.CapturedPiece;
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
        movedPieceBoard &= ~lastHistory.ToSquare;
        movedPieceBoard |= lastHistory.FromSquare;
    }

    private void UndoCastlingMove(MoveHistory lastHistory)
    {
        UndoQuietOrCaptureMove(lastHistory);

        switch (lastHistory.Move.To)
        {
            case 2:
                WhiteRooks &= ~(1UL << 3);
                WhiteRooks |= 1UL << 0;
                break;
            case 6:
                WhiteRooks &= ~(1UL << 5);
                WhiteRooks |= 1UL << 7;
                break;
            case 58:
                BlackRooks &= ~(1UL << 59);
                BlackRooks |= 1UL << 56;
                break;
            case 62:
                BlackRooks &= ~(1UL << 61);
                BlackRooks |= 1UL << 63;
                break;
        }
    }

    private void UndoEnPassantMove(MoveHistory lastHistory)
    {
        // First, undo the moving pawn's relocation.
        UndoQuietOrCaptureMove(lastHistory);

        // Calculate the square of the captured pawn.
        var capturedPawnSquare = lastHistory.Move.To + (lastHistory.Move.To > lastHistory.Move.From ? -8 : 8);
        Bitboard capturedPawnMask = 1UL << capturedPawnSquare;

        // Use the captured pawn type from the undo record.
        ref var pawnBoard = ref GetPieceBitboard(lastHistory.CapturedPieceType);
        pawnBoard |= capturedPawnMask;
    }

    private void UndoPromotionMove(MoveHistory lastHistory)
    {
        var isWhite = lastHistory.Move.To > 55;
        if (isWhite)
        {
            ref var pawnBoard = ref GetPieceBitboard(PieceType.WhitePawn);
            pawnBoard |= lastHistory.FromSquare;
        }
        else
        {
            ref var pawnBoard = ref GetPieceBitboard(PieceType.BlackPawn);
            pawnBoard |= lastHistory.FromSquare;
        }

        switch (lastHistory.Move.Type)
        {
            case MoveType.PromoteToKnight:
                if (isWhite) WhiteKnights &= ~lastHistory.ToSquare; else BlackKnights &= ~lastHistory.ToSquare;
                break;
            case MoveType.PromoteToBishop:
                if (isWhite) WhiteBishops &= ~lastHistory.ToSquare; else BlackBishops &= ~lastHistory.ToSquare;
                break;
            case MoveType.PromoteToRook:
                if (isWhite) WhiteRooks &= ~lastHistory.ToSquare; else BlackRooks &= ~lastHistory.ToSquare;
                break;
            case MoveType.PromoteToQueen:
                if (isWhite) WhiteQueens &= ~lastHistory.ToSquare; else BlackQueens &= ~lastHistory.ToSquare;
                break;
        }
    }

    private PieceType GetPieceTypeWithOverlap(Bitboard capturedPieceMask)
    {
        if ((WhitePawns &  capturedPieceMask).IsNotEmpty()) return PieceType.WhitePawn;
        if ((WhiteKnights & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteKnight;
        if ((WhiteBishops & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteBishop;
        if ((WhiteRooks  & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteRook;
        if ((WhiteQueens & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteQueen;
        if ((WhiteKing & capturedPieceMask).IsNotEmpty()) return PieceType.WhiteKing;
        if ((BlackPawns &  capturedPieceMask).IsNotEmpty()) return PieceType.BlackPawn;
        if ((BlackKnights & capturedPieceMask).IsNotEmpty()) return PieceType.BlackKnight;
        if ((BlackBishops & capturedPieceMask).IsNotEmpty()) return PieceType.BlackBishop;
        if ((BlackRooks  & capturedPieceMask).IsNotEmpty()) return PieceType.BlackRook;
        if ((BlackQueens & capturedPieceMask).IsNotEmpty()) return PieceType.BlackQueen;
        if ((BlackKing & capturedPieceMask).IsNotEmpty()) return PieceType.BlackKing;
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
        WhitePieces = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPieces = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        AllPieces = WhitePieces | BlackPieces;
    }

    public bool IsInCheck()
    {
        var kingSquare = WhiteToMove ? _whiteKing : _blackKing;
        return IsSquareAttacked(BitOperations.TrailingZeroCount(kingSquare), WhiteToMove);
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
                if (pawnSquare >= 0 && (WhitePawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }

            if (file < 7)
            {
                var pawnSquare = square - 7;
                if (pawnSquare >= 0 && (WhitePawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }
        }
        else
        {
            if (file < 7)
            {
                var pawnSquare = square + 9;
                if (pawnSquare < 64 && (BlackPawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }

            if (file > 0)
            {
                var pawnSquare = square + 7;
                if (pawnSquare < 64 && (BlackPawns & (1UL << pawnSquare)).IsNotEmpty()) return true;
            }
        }

        // Knight attacks
        Span<int> knightOffsets = [-17, -15, -10, -6, 6, 10, 15, 17];
        var knights = byWhite ? WhiteKnights : BlackKnights;
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
        var king = byWhite ? WhiteKing : BlackKing;
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
                    if (((WhiteBishops | WhiteQueens) & toMask).IsNotEmpty()) return true;
                }
                else
                {
                    if (((BlackBishops | BlackQueens) & toMask).IsNotEmpty()) return true;
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
                    if (((WhiteRooks | WhiteQueens) & toMask).IsNotEmpty()) return true;
                }
                else
                {
                    if (((BlackRooks | BlackQueens) & toMask).IsNotEmpty()) return true;
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
        var whiteMaterial = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens;
        var blackMaterial = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens;

        // Both sides have just kings
        if (whiteMaterial.IsEmpty() && blackMaterial.IsEmpty())
        {
            return true;
        }

        // If white has only its king
        if (whiteMaterial.IsEmpty())
        {
            // Black has exactly one knight
            if (blackMaterial == BlackKnights && blackMaterial.Count() == 1)
                return true;
            // or exactly one bishop
            if (blackMaterial == BlackBishops && blackMaterial.Count() == 1)
                return true;
        }

        // If black has only its king
        if (blackMaterial.IsEmpty())
        {
            // White has exactly one knight
            if (whiteMaterial == WhiteKnights && whiteMaterial.Count() == 1)
                return true;
            // or exactly one bishop
            if (whiteMaterial == WhiteBishops && whiteMaterial.Count() == 1)
                return true;
        }

        return false;
    }
}
