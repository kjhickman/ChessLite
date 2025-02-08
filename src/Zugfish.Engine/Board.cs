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

    private readonly Stack<MoveUndo> _moveHistory = new Stack<MoveUndo>();

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
        DeriveCombinedBitboards();
    }

    public Board(ReadOnlySpan<char> fen)
    {
        throw new NotImplementedException();
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

        // Save undo information
        _moveHistory.Push(new MoveUndo
        {
            CapturedPiece = capturedPieceMask,
            FromSquare = fromMask,
            ToSquare = toMask,
            Move = move
        });

        // Move the correct piece
        ref var pieceBitboard = ref GetPieceBitboard(fromMask);
        pieceBitboard &= ~fromMask;
        pieceBitboard |= toMask;

        // Remove captured piece
        if (capturedPieceMask != 0)
        {
            ref var capturedBitboard = ref GetPieceBitboard(capturedPieceMask);
            capturedBitboard &= ~capturedPieceMask;
        }

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
    // public void UnmakeMove(Move move)

    public override string ToString()
    {
        var board = new char[64];

        // Fill with empty squares
        for (var i = 0; i < 64; i++)
            board[i] = '.';

        // Place all pieces
        PlacePieces(WhitePawns, 'P');
        PlacePieces(WhiteKnights, 'N');
        PlacePieces(WhiteBishops, 'B');
        PlacePieces(WhiteRooks, 'R');
        PlacePieces(WhiteQueens, 'Q');
        PlacePieces(WhiteKing, 'K');

        PlacePieces(BlackPawns, 'p');
        PlacePieces(BlackKnights, 'n');
        PlacePieces(BlackBishops, 'b');
        PlacePieces(BlackRooks, 'r');
        PlacePieces(BlackQueens, 'q');
        PlacePieces(BlackKing, 'k');

        // Build board string
        var sb = new StringBuilder();
        for (var rank = 7; rank >= 0; rank--)  // Print top-down
        {
            for (var file = 0; file < 8; file++)
            {
                sb.Append(board[rank * 8 + file]);
                sb.Append(' '); // Space for readability
            }
            sb.AppendLine();
        }
        return sb.ToString();

        // Helper to place pieces
        void PlacePieces(Bitboard bitboard, char pieceChar)
        {
            var value = bitboard.Value;
            for (var i = 0; i < 64; i++)
            {
                if ((value & (1UL << i)) != 0)
                    board[i] = pieceChar;
            }
        }
    }
}
