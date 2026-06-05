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

    /// <summary>Gets or sets the fullmove number, starting at 1 and incrementing after Black's move.</summary>
    public int FullmoveNumber { get; set; }

    /// <summary>Gets or sets the Zobrist hash of the current position, used for transposition tables and repetition detection.</summary>
    public ulong ZobristHash { get; set; }

    #endregion

    #region Derived Bitboards

    /// <summary>Gets a bitboard representing all white pieces.</summary>
    public Bitboard WhitePieces { get; internal set; }

    /// <summary>Gets a bitboard representing all black pieces.</summary>
    public Bitboard BlackPieces { get; internal set; }

    /// <summary>Gets a bitboard representing all occupied squares.</summary>
    public Bitboard AllPieces { get; internal set; }

    #endregion

    #region Attacks & Pins

    internal Bitboard PinnedPieces;
    internal Bitboard WhiteAttacks;
    internal Bitboard WhiteAttacksWithoutBlackKing;
    internal Bitboard BlackAttacks;
    internal Bitboard BlackAttacksWithoutWhiteKing;

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
        : this(initializeFromFen: true)
    {
    }

    internal Position(bool initializeFromFen)
    {
        Mailbox = new PieceType[64];
        if (!initializeFromFen)
        {
            return;
        }

        if (!FenParser.Parse(Constants.StartingPosition, this))
        {
            throw new InvalidOperationException("Failed to initialize starting position.");
        }
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

    internal void Reset()
    {
        WhitePawns = 0;
        WhiteKnights = 0;
        WhiteBishops = 0;
        WhiteRooks = 0;
        WhiteQueens = 0;
        WhiteKing = 0;
        BlackPawns = 0;
        BlackKnights = 0;
        BlackBishops = 0;
        BlackRooks = 0;
        BlackQueens = 0;
        BlackKing = 0;
        WhiteToMove = true;
        CastlingRights = CastlingRights.None;
        EnPassantTarget = Square.None;
        HalfmoveClock = 0;
        FullmoveNumber = 1;
        ZobristHash = 0;
        WhitePieces = 0;
        BlackPieces = 0;
        AllPieces = 0;
        PinnedPieces = 0;
        WhiteAttacks = 0;
        WhiteAttacksWithoutBlackKing = 0;
        BlackAttacks = 0;
        BlackAttacksWithoutWhiteKing = 0;
        Array.Fill(Mailbox, PieceType.None);
    }

    /// <summary>
    /// Recalculates all attack bitboards for both sides based on the current piece positions.
    /// </summary>
    public void UpdateAttacks()
    {
        UpdateWhiteAttacks();
        UpdateBlackAttacks();
    }

    private void UpdateWhiteAttacks()
    {
        var whitePawnAttacks = AttackGeneration.CalculatePawnAttacks(WhitePawns, forWhite: true);
        var whiteKnightAttacks = AttackGeneration.CalculateKnightAttacks(WhiteKnights);
        var whiteKingAttacks = AttackGeneration.CalculateKingAttacks(WhiteKing);
        var whiteSliderAttacks = AttackGeneration.CalculateBishopAttacks(WhiteBishops, AllPieces)
            | AttackGeneration.CalculateRookAttacks(WhiteRooks, AllPieces)
            | AttackGeneration.CalculateQueenAttacks(WhiteQueens, AllPieces);
        var whiteSliderAttacksWithoutBlackKing = AttackGeneration.CalculateBishopAttacks(WhiteBishops, AllPieces.ClearSquares(BlackKing))
            | AttackGeneration.CalculateRookAttacks(WhiteRooks, AllPieces.ClearSquares(BlackKing))
            | AttackGeneration.CalculateQueenAttacks(WhiteQueens, AllPieces.ClearSquares(BlackKing));
        WhiteAttacks = whitePawnAttacks | whiteKnightAttacks | whiteKingAttacks | whiteSliderAttacks;
        WhiteAttacksWithoutBlackKing = whitePawnAttacks | whiteKnightAttacks | whiteKingAttacks | whiteSliderAttacksWithoutBlackKing;
    }

    private void UpdateBlackAttacks()
    {
        var blackPawnAttacks = AttackGeneration.CalculatePawnAttacks(BlackPawns, forWhite: false);
        var blackKnightAttacks = AttackGeneration.CalculateKnightAttacks(BlackKnights);
        var blackKingAttacks = AttackGeneration.CalculateKingAttacks(BlackKing);
        var blackSliderAttacks = AttackGeneration.CalculateBishopAttacks(BlackBishops, AllPieces)
            | AttackGeneration.CalculateRookAttacks(BlackRooks, AllPieces)
            | AttackGeneration.CalculateQueenAttacks(BlackQueens, AllPieces);
        var blackSliderAttacksWithoutWhiteKing = AttackGeneration.CalculateBishopAttacks(BlackBishops, AllPieces.ClearSquares(WhiteKing))
            | AttackGeneration.CalculateRookAttacks(BlackRooks, AllPieces.ClearSquares(WhiteKing))
            | AttackGeneration.CalculateQueenAttacks(BlackQueens, AllPieces.ClearSquares(WhiteKing));
        BlackAttacks = blackPawnAttacks | blackKnightAttacks | blackKingAttacks | blackSliderAttacks;
        BlackAttacksWithoutWhiteKing = blackPawnAttacks | blackKnightAttacks | blackKingAttacks | blackSliderAttacksWithoutWhiteKing;
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
        var king = WhiteToMove ? WhiteKing : BlackKing;
        if (king.IsEmpty()) return pinnedPieces;

        var kingSquare = king.GetFirstSquare();
        var friendlyPieces = WhiteToMove ? WhitePieces : BlackPieces;
        var diagonalPinners = WhiteToMove
            ? BlackBishops | BlackQueens
            : WhiteBishops | WhiteQueens;
        var orthogonalPinners = WhiteToMove
            ? BlackRooks | BlackQueens
            : WhiteRooks | WhiteQueens;

        var candidatePinners = (MagicBitboards.GetBishopAttacks(kingSquare, 0) & diagonalPinners)
            | (MagicBitboards.GetRookAttacks(kingSquare, 0) & orthogonalPinners);

        while (candidatePinners.IsNotEmpty())
        {
            var pinnerSquare = candidatePinners.GetFirstSquare();
            var blockers = AttackTables.RayBetween[(int)kingSquare][(int)pinnerSquare] & AllPieces;
            if (blockers.Count() == 1 && blockers.Intersects(friendlyPieces))
            {
                pinnedPieces |= blockers;
            }

            candidatePinners &= candidatePinners - 1;
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
            FullmoveNumber = FullmoveNumber,
            ZobristHash = ZobristHash,

            // Copy derived bitboards
            WhitePieces = WhitePieces,
            BlackPieces = BlackPieces,
            AllPieces = AllPieces,
            // Copy attack bitboards
            PinnedPieces = PinnedPieces,
            WhiteAttacks = WhiteAttacks,
            WhiteAttacksWithoutBlackKing = WhiteAttacksWithoutBlackKing,
            BlackAttacks = BlackAttacks,
            BlackAttacksWithoutWhiteKing = BlackAttacksWithoutWhiteKing,
        };

        // Copy mailbox
        Array.Copy(Mailbox, clone.Mailbox, 64);

        return clone;
    }
}
