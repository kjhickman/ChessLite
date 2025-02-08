using System.Text;

namespace Zugfish.Engine;

public class Board
{
    public Bitboard WhitePawns { get; private set; }
    public Bitboard WhiteKnights { get; private set; }
    public Bitboard WhiteBishops { get; private set; }
    public Bitboard WhiteRooks { get; private set; }
    public Bitboard WhiteQueens { get; private set; }
    public Bitboard WhiteKing { get; private set; }

    public Bitboard BlackPawns { get; private set; }
    public Bitboard BlackKnights { get; private set; }
    public Bitboard BlackBishops { get; private set; }
    public Bitboard BlackRooks { get; private set; }
    public Bitboard BlackQueens { get; private set; }
    public Bitboard BlackKing { get; private set; }

    public Bitboard WhitePieces { get; private set; }
    public Bitboard BlackPieces { get; private set; }
    public Bitboard AllPieces { get; private set; }

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
    
    // public void MakeMove(Move move)
    // public void UnmakeMove(Move move)

    public override string ToString()
    {
        var board = new char[64];

        // Fill with empty squares
        for (var i = 0; i < 64; i++)
            board[i] = '.';

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
    }

    
}
