using System.Text;

namespace Zugfish.Engine;

public class Board
{
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
    public Bitboard AllPieces { get; private set; }

    private readonly Stack<MoveUndo> _moveHistory = new();
    public ushort CastlingRights { get; private set; }

    public Board()
    {
        WhitePawns = new Bitboard(0xFF00);
        WhiteKnights = new Bitboard(0x42);
        WhiteBishops = new Bitboard(0x24);
        WhiteRooks = new Bitboard(0x81);
        WhiteQueens = new Bitboard(0x8);
        WhiteKing = new Bitboard(0x10);
        BlackPawns = new Bitboard(0xFF000000000000);
        BlackKnights = new Bitboard(0x4200000000000000);
        BlackBishops = new Bitboard(0x2400000000000000);
        BlackRooks = new Bitboard(0x8100000000000000);
        BlackQueens = new Bitboard(0x800000000000000);
        BlackKing = new Bitboard(0x1000000000000000);
        CastlingRights = 0b1111;
        DeriveCombinedBitboards();
    }

    public Board(ReadOnlySpan<char> fen)
    {
        var enumerator = fen.Split(' ');
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
                Bitboard pieceMask = 1UL << square++;
                switch (c)
                {
                    case 'P':
                        WhitePawns |= pieceMask;
                        break;
                    case 'N':
                        WhiteKnights |= pieceMask;
                        break;
                    case 'B':
                        WhiteBishops |= pieceMask;
                        break;
                    case 'R':
                        WhiteRooks |= pieceMask;
                        break;
                    case 'Q':
                        WhiteQueens |= pieceMask;
                        break;
                    case 'K':
                        WhiteKing |= pieceMask;
                        break;
                    case 'p':
                        BlackPawns |= pieceMask;
                        break;
                    case 'n':
                        BlackKnights |= pieceMask;
                        break;
                    case 'b':
                        BlackBishops |= pieceMask;
                        break;
                    case 'r':
                        BlackRooks |= pieceMask;
                        break;
                    case 'q':
                        BlackQueens |= pieceMask;
                        break;
                    case 'k':
                        BlackKing |= pieceMask;
                        break;
                    default:
                        throw new ArgumentException($"Invalid FEN piece: {c}");
                }
            }
        }

        var activeColor = fen[enumerator.Current]; enumerator.MoveNext();

        // Parse castling rights
        var castlingRights = fen[enumerator.Current]; enumerator.MoveNext();

        if (castlingRights.IndexOf('K') != -1) CastlingRights |= 0b0001;
        if (castlingRights.IndexOf('Q') != -1) CastlingRights |= 0b0010;
        if (castlingRights.IndexOf('k') != -1) CastlingRights |= 0b0100;
        if (castlingRights.IndexOf('q') != -1) CastlingRights |= 0b1000;

        // Parse en passant square
        var enPassantSquare = fen[enumerator.Current]; enumerator.MoveNext();
        // if (enPassantSquare is not "-")
        // {
        //     int file = enPassantSquare[0] - 'a';
        //     int rank = enPassantSquare[1] - '1';
        //     EnPassantTarget = 1UL << (rank * 8 + file);
        // }
        // else
        // {
        //     EnPassantTarget = 0;
        // }

        var halfmoveClock = fen[enumerator.Current]; enumerator.MoveNext();

        var fullmoveNumber = fen[enumerator.Current]; enumerator.MoveNext();

        // Update combined bitboards
        DeriveCombinedBitboards();
    }
    
    private void DeriveCombinedBitboards()
    {
        WhitePieces = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPieces = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        AllPieces = WhitePieces | BlackPieces;
    }
    
    public void MakeMove(Move move)
    {
        var from = move.From;
        var to = move.To;

        var fromMask = new Bitboard(1UL << from);
        var toMask = new Bitboard(1UL << to);

        // Identify which piece moved
        var capturedPieceMask = AllPieces & toMask;

        // Move the correct piece
        ref var pieceBitboard = ref GetPieceBitboard(fromMask);
        pieceBitboard &= ~fromMask;
        pieceBitboard |= toMask;

        // Remove captured piece
        if (capturedPieceMask != 0)
        {
            ref var capturedBitboard = ref GetPieceBitboard(capturedPieceMask);
            capturedBitboard &= ~capturedPieceMask;

            // Detect if a rook was captured and update castling rights
            if ((capturedPieceMask & WhiteRooks) != 0) // White rook captured
            {
                if ((capturedPieceMask & (1UL << 0)) != 0) CastlingRights &= 0b1101; // White queenside rook captured
                if ((capturedPieceMask & (1UL << 7)) != 0) CastlingRights &= 0b1110; // White kingside rook captured
            }
            else if ((capturedPieceMask & BlackRooks) != 0) // Black rook captured
            {
                if ((capturedPieceMask & (1UL << 56)) != 0) CastlingRights &= 0b0111; // Black queenside rook captured
                if ((capturedPieceMask & (1UL << 63)) != 0) CastlingRights &= 0b1011; // Black kingside rook captured
            }
        }

        // Need to move the rook for castling
        if (move.Flag == (int)MoveType.Castling)
        {
            switch (to)
            {
                case 2: // White Queen-side castling
                    WhiteRooks &= ~(1UL << 0); // Remove rook from a1
                    WhiteRooks |= 1UL << 3;  // Place rook on d1
                    break;
                case 6: // White King-side castling
                    WhiteRooks &= ~(1UL << 7); // Remove rook from h1
                    WhiteRooks |= 1UL << 5;  // Place rook on f1
                    break;
                case 58: // Black Queen-side castling
                    BlackRooks &= ~(1UL << 56); // Remove rook from a8
                    BlackRooks |= 1UL << 59;  // Place rook on d8
                    break;
                case 62: // Black King-side castling
                    BlackRooks &= ~(1UL << 63); // Remove rook from h8
                    BlackRooks |= 1UL << 61;  // Place rook on f8
                    break;
            }
        }

        switch (from)
        {
            // Update castling rights (if king or rook moves)
            case 4 or 60:
                CastlingRights &= 0b1100; // King moved
                break;
            case 0 or 56:
                CastlingRights &= 0b1110; // Queen-side rook moved
                break;
            case 7 or 63:
                CastlingRights &= 0b1101; // King-side rook moved
                break;
        }

        // Save undo information
        _moveHistory.Push(new MoveUndo
        {
            CapturedPiece = capturedPieceMask,
            FromSquare = fromMask,
            ToSquare = toMask,
            Move = move,
            PreviousCastlingRights = CastlingRights
        });

        // Recalculate combined bitboards
        DeriveCombinedBitboards();
    }

    public void UnmakeMove()
    {
        if (_moveHistory.Count == 0)
            throw new InvalidOperationException("No move to unmake.");

        var lastMove = _moveHistory.Pop();

        // Restore the moved piece to its original position
        ref var movedPieceBitboard = ref GetPieceBitboard(lastMove.ToSquare);
        movedPieceBitboard &= ~lastMove.ToSquare;
        movedPieceBitboard |= lastMove.FromSquare;

        // Restore captured piece (if any)
        if (lastMove.CapturedPiece != 0)
        {
            ref var capturedPieceBitboard = ref GetPieceBitboard(lastMove.CapturedPiece);
            capturedPieceBitboard |= lastMove.CapturedPiece;
        }

        CastlingRights = lastMove.PreviousCastlingRights;

        if (lastMove.Move.Flag == (int)MoveType.Castling)
        {
            switch (lastMove.Move.To)
            {
                // White Queen-side
                case 2:
                    WhiteRooks &= ~(1UL << 3); // Remove rook from d1
                    WhiteRooks |= (1UL << 0);  // Restore rook to a1
                    break;
                // White King-side
                case 6:
                    WhiteRooks &= ~(1UL << 5); // Remove rook from f1
                    WhiteRooks |= (1UL << 7);  // Restore rook to h1
                    break;
                // Black Queen-side
                case 58:
                    BlackRooks &= ~(1UL << 59); // Remove rook from d8
                    BlackRooks |= (1UL << 56);  // Restore rook to a8
                    break;
                // Black King-side
                case 62:
                    BlackRooks &= ~(1UL << 61); // Remove rook from f8
                    BlackRooks |= (1UL << 63);  // Restore rook to h8
                    break;
            }
        }

        CastlingRights = lastMove.Move.Flag == (int)MoveType.Castling ? (ushort)(CastlingRights | lastMove.Move.Flag) : CastlingRights;

        // Recalculate combined bitboards
        DeriveCombinedBitboards();
    }

    private ref Bitboard GetPieceBitboard(Bitboard bitboard)
    {
        if ((WhitePawns & bitboard) != 0) return ref _whitePawns;
        if ((WhiteKnights & bitboard) != 0) return ref _whiteKnights;
        if ((WhiteBishops & bitboard) != 0) return ref _whiteBishops;
        if ((WhiteRooks & bitboard) != 0) return ref _whiteRooks;
        if ((WhiteQueens & bitboard) != 0) return ref _whiteQueens;
        if ((WhiteKing & bitboard) != 0) return ref _whiteKing;

        if ((BlackPawns & bitboard) != 0) return ref _blackPawns;
        if ((BlackKnights & bitboard) != 0) return ref _blackKnights;
        if ((BlackBishops & bitboard) != 0) return ref _blackBishops;
        if ((BlackRooks & bitboard) != 0) return ref _blackRooks;
        if ((BlackQueens & bitboard) != 0) return ref _blackQueens;
        if ((BlackKing & bitboard) != 0) return ref _blackKing;

        throw new InvalidOperationException("No matching piece found for given bitboard.");
    }

}
