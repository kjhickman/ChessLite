using System.Text;
using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.Primitives;

namespace ChessLite.State;

/// <summary>
/// Represents a chess position, including piece placement, side to move, castling rights, and other state.
/// </summary>
public class Position
{
    #region Fields and Properties

    /// <summary>Bitboard representing the positions of all white pawns.</summary>
    public Bitboard WhitePawns;

    /// <summary>Bitboard representing the positions of all white knights.</summary>
    public Bitboard WhiteKnights;

    /// <summary>Bitboard representing the positions of all white bishops.</summary>
    public Bitboard WhiteBishops;

    /// <summary>Bitboard representing the positions of all white rooks.</summary>
    public Bitboard WhiteRooks;

    /// <summary>Bitboard representing the positions of all white queens.</summary>
    public Bitboard WhiteQueens;

    /// <summary>Bitboard representing the position of the white king.</summary>
    public Bitboard WhiteKing;

    /// <summary>Bitboard representing the positions of all black pawns.</summary>
    public Bitboard BlackPawns;

    /// <summary>Bitboard representing the positions of all black knights.</summary>
    public Bitboard BlackKnights;

    /// <summary>Bitboard representing the positions of all black bishops.</summary>
    public Bitboard BlackBishops;

    /// <summary>Bitboard representing the positions of all black rooks.</summary>
    public Bitboard BlackRooks;

    /// <summary>Bitboard representing the positions of all black queens.</summary>
    public Bitboard BlackQueens;

    /// <summary>Bitboard representing the position of the black king.</summary>
    public Bitboard BlackKing;

    /// <summary>Gets or sets a value indicating whether it is white's turn to move.</summary>
    public bool WhiteToMove { get; set; }

    /// <summary>Gets or sets the current castling rights for both sides.</summary>
    public CastlingRights CastlingRights { get; set; }

    /// <summary>Gets or sets the en passant target square, or <see cref="Square.None"/> if no en passant is possible.</summary>
    public Square EnPassantTarget { get; set; }

    /// <summary>Gets or sets the halfmove clock, used for the fifty-move rule.</summary>
    public int HalfmoveClock { get; set; }

    /// <summary>Gets or sets the Zobrist hash of the current position, used for transposition tables and repetition detection.</summary>
    public ulong ZobristHash { get; set; }

    #endregion

    #region Derived Bitboards

    internal Bitboard WhitePieces { get; set; }
    internal Bitboard BlackPieces { get; set; }
    internal Bitboard AllPieces { get; set; }

    #endregion

    #region Attacks & Pins

    internal Bitboard PinnedPieces;
    internal Bitboard WhiteAttacks;
    internal Bitboard WhiteAttacksWithoutBlackKing;
    internal Bitboard WhitePawnAttacks;
    internal Bitboard WhiteKnightAttacks;
    internal Bitboard WhiteKingAttacks;
    internal Bitboard BlackAttacks;
    internal Bitboard BlackAttacksWithoutWhiteKing;
    internal Bitboard BlackPawnAttacks;
    internal Bitboard BlackKnightAttacks;
    internal Bitboard BlackKingAttacks;

    #endregion

    #region Mailbox

    /// <summary>
    /// Array-based board representation mapping each square index (0-63) to the piece type occupying it.
    /// </summary>
    public PieceType[] Mailbox = new PieceType[64];

    #endregion

    /// <summary>
    /// Default constructor: sets up the standard starting position.
    /// </summary>
    public Position()
    {
        // todo: don't use ParseFen here. Directly set up the starting position
        var position = ParseFen(Constants.StartingPosition);
        WhitePawns = position.WhitePawns;
        WhiteKnights = position.WhiteKnights;
        WhiteBishops = position.WhiteBishops;
        WhiteRooks = position.WhiteRooks;
        WhiteQueens = position.WhiteQueens;
        WhiteKing = position.WhiteKing;
        BlackPawns = position.BlackPawns;
        BlackKnights = position.BlackKnights;
        BlackBishops = position.BlackBishops;
        BlackRooks = position.BlackRooks;
        BlackQueens = position.BlackQueens;
        BlackKing = position.BlackKing;
        WhiteToMove = position.WhiteToMove;
        CastlingRights = position.CastlingRights;
        EnPassantTarget = position.EnPassantTarget;
        HalfmoveClock = position.HalfmoveClock;
        WhitePieces = position.WhitePieces;
        BlackPieces = position.BlackPieces;
        AllPieces = position.AllPieces;
        PinnedPieces = position.PinnedPieces;
        WhiteAttacks = position.WhiteAttacks;
        WhiteAttacksWithoutBlackKing = position.WhiteAttacksWithoutBlackKing;
        WhitePawnAttacks = position.WhitePawnAttacks;
        WhiteKnightAttacks = position.WhiteKnightAttacks;
        WhiteKingAttacks = position.WhiteKingAttacks;
        BlackAttacks = position.BlackAttacks;
        BlackAttacksWithoutWhiteKing = position.BlackAttacksWithoutWhiteKing;
        BlackPawnAttacks = position.BlackPawnAttacks;
        BlackKnightAttacks = position.BlackKnightAttacks;
        BlackKingAttacks = position.BlackKingAttacks;
        ZobristHash = position.ZobristHash;
        Array.Copy(position.Mailbox, Mailbox, 64);
    }

    public static bool TryParseFen(ReadOnlySpan<char> fen, out Position? position)
    {
        position = new Position { Mailbox = new PieceType[64] };

        try
        {
            if (!FenParser.Parse(fen, position))
            {
                position = null;
                return false;
            }
            return true;
        }
        catch
        {
            position = null;
            return false;
        }
    }

    public static Position ParseFen(ReadOnlySpan<char> fen)
    {
        var position = new Position { Mailbox = new PieceType[64] };
        if (!FenParser.Parse(fen, position))
        {
            throw new ArgumentException("Invalid FEN string", nameof(fen));
        }
        return position;
    }

    /// <summary>
    /// Returns a human-readable string representation of the board.
    /// </summary>
    /// <returns>A string displaying the board with file and rank labels.</returns>
    public override string ToString()
    {
        Span<char> boardArray = stackalloc char[64];

        // Fill with empty squares.
        for (var i = 0; i < 64; i++)
            boardArray[i] = '.';

        // Place white pieces.
        PlacePieces(WhitePawns, 'P', ref boardArray);
        PlacePieces(WhiteKnights, 'N', ref boardArray);
        PlacePieces(WhiteBishops, 'B', ref boardArray);
        PlacePieces(WhiteRooks, 'R', ref boardArray);
        PlacePieces(WhiteQueens, 'Q', ref boardArray);
        PlacePieces(WhiteKing, 'K', ref boardArray);

        // Place black pieces.
        PlacePieces(BlackPawns, 'p', ref boardArray);
        PlacePieces(BlackKnights, 'n', ref boardArray);
        PlacePieces(BlackBishops, 'b', ref boardArray);
        PlacePieces(BlackRooks, 'r', ref boardArray);
        PlacePieces(BlackQueens, 'q', ref boardArray);
        PlacePieces(BlackKing, 'k', ref boardArray);

        // Build board string.
        var sb = new StringBuilder();
        sb.AppendLine("  a b c d e f g h");
        sb.AppendLine("  ----------------");
        for (var rank = 7; rank >= 0; rank--)  // Top-down rendering.
        {
            sb.Append($"{rank + 1}| ");
            for (var file = 0; file < 8; file++)
            {
                sb.Append(boardArray[rank * 8 + file]);
                sb.Append(' ');
            }
            sb.AppendLine("|");
        }
        sb.AppendLine("  ----------------");
        return sb.ToString();

        // Local function to place pieces based on bitboard.
        void PlacePieces(Bitboard bitboard, char pieceChar, ref Span<char> boardArray)
        {
            for (var i = 0; i < 64; i++)
            {
                if (bitboard.Intersects((Square)i))
                {
                    boardArray[i] = pieceChar;
                }
            }
        }
    }

    /// <summary>
    /// Gets a reference to the bitboard for the specified piece type.
    /// </summary>
    /// <param name="pieceType">The piece type to get the bitboard for.</param>
    /// <returns>A reference to the bitboard containing pieces of the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="pieceType"/> is <see cref="PieceType.None"/>.</exception>
    public ref Bitboard GetPieceBitboard(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.WhitePawn: return ref WhitePawns;
            case PieceType.WhiteKnight: return ref WhiteKnights;
            case PieceType.WhiteBishop: return ref WhiteBishops;
            case PieceType.WhiteRook: return ref WhiteRooks;
            case PieceType.WhiteQueen: return ref WhiteQueens;
            case PieceType.WhiteKing: return ref WhiteKing;
            case PieceType.BlackPawn: return ref BlackPawns;
            case PieceType.BlackKnight: return ref BlackKnights;
            case PieceType.BlackBishop: return ref BlackBishops;
            case PieceType.BlackRook: return ref BlackRooks;
            case PieceType.BlackQueen: return ref BlackQueens;
            case PieceType.BlackKing: return ref BlackKing;
            case PieceType.None:
            default: throw new InvalidOperationException("No matching piece found for given piece type.");
        }
    }

    internal void UpdateCombinedBitboards()
    {
        WhitePieces = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPieces = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        AllPieces = WhitePieces | BlackPieces;
    }

    /// <summary>
    /// Recalculates all attack bitboards for both sides based on the current piece positions.
    /// </summary>
    public void UpdateAttacks()
    {
        WhiteAttacks = AttackGeneration.CalculateAttacks(this, forWhite: true);
        WhiteAttacksWithoutBlackKing = AttackGeneration.CalculateAttacksWithoutOpposingKing(this, forWhite: true);
        WhitePawnAttacks = AttackGeneration.CalculatePawnAttacks(WhitePawns, forWhite: true);
        WhiteKnightAttacks = AttackGeneration.CalculateKnightAttacks(WhiteKnights);
        WhiteKingAttacks = AttackGeneration.CalculateKingAttacks(WhiteKing);

        BlackAttacks = AttackGeneration.CalculateAttacks(this, forWhite: false);
        BlackAttacksWithoutWhiteKing = AttackGeneration.CalculateAttacksWithoutOpposingKing(this, forWhite: false);
        BlackPawnAttacks = AttackGeneration.CalculatePawnAttacks(BlackPawns, forWhite: false);
        BlackKnightAttacks = AttackGeneration.CalculateKnightAttacks(BlackKnights);
        BlackKingAttacks = AttackGeneration.CalculateKingAttacks(BlackKing);
    }

    /// <summary>
    /// Recalculates the bitboard of pinned pieces for the side to move.
    /// </summary>
    public void UpdatePinnedPieces()
    {
        PinnedPieces = ComputePinnedPieces();
    }

    /// <summary>
    /// Rebuilds the mailbox array from the current bitboard state.
    /// </summary>
    public void UpdateMailbox()
    {
        Array.Fill(Mailbox, PieceType.None);
        SetMailboxForPieces(WhitePawns, PieceType.WhitePawn);
        SetMailboxForPieces(WhiteKnights, PieceType.WhiteKnight);
        SetMailboxForPieces(WhiteBishops, PieceType.WhiteBishop);
        SetMailboxForPieces(WhiteRooks, PieceType.WhiteRook);
        SetMailboxForPieces(WhiteQueens, PieceType.WhiteQueen);
        SetMailboxForPieces(WhiteKing, PieceType.WhiteKing);
        SetMailboxForPieces(BlackPawns, PieceType.BlackPawn);
        SetMailboxForPieces(BlackKnights, PieceType.BlackKnight);
        SetMailboxForPieces(BlackBishops, PieceType.BlackBishop);
        SetMailboxForPieces(BlackRooks, PieceType.BlackRook);
        SetMailboxForPieces(BlackQueens, PieceType.BlackQueen);
        SetMailboxForPieces(BlackKing, PieceType.BlackKing);
    }

    private void SetMailboxForPieces(Bitboard bb, PieceType pieceType)
    {
        while (bb != 0)
        {
            var square = bb.GetFirstSquare();
            Mailbox[(int)square] = pieceType;
            bb &= bb - 1;
        }
    }

    private Bitboard ComputePinnedPieces()
    {
        Bitboard pinnedPieces = 0;
        var kingSquare = WhiteToMove ? WhiteKing.GetFirstSquare() : BlackKing.GetFirstSquare();
        var friendlyPieces = WhiteToMove ? WhitePieces : BlackPieces;

        Span<(int fileDir, int rankDir)> directions =
        [
            (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1)
        ];

        for (var i = 0; i < directions.Length; i++)
        {
            var (fileDir, rankDir) = directions[i];
            Bitboard potentiallyPinned = 0;
            var kingFile = kingSquare.GetFile();
            var kingRank = kingSquare.GetRank();
            var currentFile = kingFile + fileDir;
            var currentRank = kingRank + rankDir;

            while (currentFile is >= 0 and < 8 && currentRank is >= 0 and < 8)
            {
                var currentSquare = (Square)(currentRank * 8 + currentFile);
                var squareMask = Bitboard.Mask(currentSquare);

                if ((friendlyPieces & squareMask).IsNotEmpty())
                {
                    if (potentiallyPinned != 0) break; // Second friendly piece, no pin possible

                    potentiallyPinned = squareMask;
                }
                else if ((AllPieces & squareMask).IsNotEmpty())
                {
                    // Found enemy piece
                    var isDiagonal = fileDir != 0 && rankDir != 0;
                    var enemySliders = WhiteToMove
                        ? (isDiagonal ? BlackBishops | BlackQueens : BlackRooks | BlackQueens)
                        : (isDiagonal ? WhiteBishops | WhiteQueens : WhiteRooks | WhiteQueens);

                    if (potentiallyPinned != 0 && (squareMask & enemySliders).IsNotEmpty())
                    {
                        // Pin confirmed
                        pinnedPieces |= potentiallyPinned;
                    }

                    break;
                }

                currentFile += fileDir;
                currentRank += rankDir;
            }
        }

        return pinnedPieces;
    }

    /// <summary>
    /// Creates a deep copy of the current position.
    /// </summary>
    /// <returns>A new <see cref="Position"/> instance with identical state.</returns>
    public Position Clone()
    {
        var clone = new Position
        {
            // Copy all bitboards
            WhitePawns = WhitePawns,
            WhiteKnights = WhiteKnights,
            WhiteBishops = WhiteBishops,
            WhiteRooks = WhiteRooks,
            WhiteQueens = WhiteQueens,
            WhiteKing = WhiteKing,
            BlackPawns = BlackPawns,
            BlackKnights = BlackKnights,
            BlackBishops = BlackBishops,
            BlackRooks = BlackRooks,
            BlackQueens = BlackQueens,
            BlackKing = BlackKing,

            // Copy state variables
            WhiteToMove = WhiteToMove,
            CastlingRights = CastlingRights,
            EnPassantTarget = EnPassantTarget,
            HalfmoveClock = HalfmoveClock,
            ZobristHash = ZobristHash,

            // Copy derived bitboards
            WhitePieces = WhitePieces,
            BlackPieces = BlackPieces,
            AllPieces = AllPieces,

            // Copy attack bitboards
            PinnedPieces = PinnedPieces,
            WhiteAttacks = WhiteAttacks,
            WhiteAttacksWithoutBlackKing = WhiteAttacksWithoutBlackKing,
            WhitePawnAttacks = WhitePawnAttacks,
            WhiteKnightAttacks = WhiteKnightAttacks,
            WhiteKingAttacks = WhiteKingAttacks,
            BlackAttacks = BlackAttacks,
            BlackAttacksWithoutWhiteKing = BlackAttacksWithoutWhiteKing,
            BlackPawnAttacks = BlackPawnAttacks,
            BlackKnightAttacks = BlackKnightAttacks,
            BlackKingAttacks = BlackKingAttacks,
        };

        // Copy mailbox
        Array.Copy(Mailbox, clone.Mailbox, 64);

        return clone;
    }
}
