using System.Text;
using Veloce.Core;
using Veloce.Movement;
using Veloce.Search;

namespace Veloce.State;

public class Position
{
    #region Fields and Properties

    public Bitboard WhitePawns;
    public Bitboard WhiteKnights;
    public Bitboard WhiteBishops;
    public Bitboard WhiteRooks;
    public Bitboard WhiteQueens;
    public Bitboard WhiteKing;

    public Bitboard BlackPawns;
    public Bitboard BlackKnights;
    public Bitboard BlackBishops;
    public Bitboard BlackRooks;
    public Bitboard BlackQueens;
    public Bitboard BlackKing;

    public bool WhiteToMove { get; set; }
    public CastlingRights CastlingRights { get; set; }
    public Square EnPassantTarget { get; set; }
    public int HalfmoveClock { get; set; }
    public ulong ZobristHash { get; set; }

    #endregion

    #region Derived Bitboards

    public Bitboard WhitePieces { get; set; }
    public Bitboard BlackPieces { get; set; }
    public Bitboard AllPieces { get; set; }

    #endregion

    #region Attacks & Pins

    public Bitboard PinnedPieces;
    public Bitboard WhiteAttacks;
    public Bitboard WhiteAttacksWithoutBlackKing;
    public Bitboard WhitePawnAttacks;
    public Bitboard WhiteKnightAttacks;
    public Bitboard WhiteKingAttacks;
    public Bitboard BlackAttacks;
    public Bitboard BlackAttacksWithoutWhiteKing;
    public Bitboard BlackPawnAttacks;
    public Bitboard BlackKnightAttacks;
    public Bitboard BlackKingAttacks;

    #endregion

    /// <summary>
    /// Default constructor: sets up the standard starting position.
    /// </summary>
    public Position() : this(Constants.StartingPosition)
    {
        UpdateCombinedBitboards();
        UpdateAttacks();
        UpdatePinnedPieces();
        ZobristHash = Zobrist.ComputeHash(this);
    }

    /// <summary>
    /// Constructor that sets up a position from a FEN string.
    /// </summary>
    public Position(ReadOnlySpan<char> fen)
    {
        var fenData = FenParser.ParseFen(fen);
        WhitePawns = fenData.WhitePawns;
        WhiteKnights = fenData.WhiteKnights;
        WhiteBishops = fenData.WhiteBishops;
        WhiteRooks = fenData.WhiteRooks;
        WhiteQueens = fenData.WhiteQueens;
        WhiteKing = fenData.WhiteKing;
        BlackPawns = fenData.BlackPawns;
        BlackKnights = fenData.BlackKnights;
        BlackBishops = fenData.BlackBishops;
        BlackRooks = fenData.BlackRooks;
        BlackQueens = fenData.BlackQueens;
        BlackKing = fenData.BlackKing;
        WhiteToMove = fenData.WhiteToMove;
        CastlingRights = fenData.CastlingRights;
        EnPassantTarget = fenData.EnPassantTarget;
        HalfmoveClock = fenData.HalfmoveClock;

        // Update combined bitboards
        UpdateCombinedBitboards();
        UpdateAttacks();
        UpdatePinnedPieces();
        ZobristHash = Zobrist.ComputeHash(this);
    }

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

    private void UpdateCombinedBitboards()
    {
        WhitePieces = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPieces = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        AllPieces = WhitePieces | BlackPieces;
    }

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

    public void UpdatePinnedPieces()
    {
        PinnedPieces = ComputePinnedPieces();
    }

    private Bitboard ComputePinnedPieces()
    {
        Bitboard pinnedPieces = 0;
        var kingSquare = WhiteToMove ? WhiteKing.GetFirstSquare() : BlackKing.GetFirstSquare();
        var friendlyPieces = WhiteToMove ? WhitePieces : BlackPieces;

        Span<(int fileDir, int rankDir)> directions = stackalloc (int, int)[]
        {
            (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1),
        };

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
            WhitePawnAttacks = WhitePawnAttacks,
            WhiteKnightAttacks = WhiteKnightAttacks,
            WhiteKingAttacks = WhiteKingAttacks,
            BlackAttacks = BlackAttacks,
            BlackPawnAttacks = BlackPawnAttacks,
            BlackKnightAttacks = BlackKnightAttacks,
            BlackKingAttacks = BlackKingAttacks,
        };

        return clone;
    }
}
