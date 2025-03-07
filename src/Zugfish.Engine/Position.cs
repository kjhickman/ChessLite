using System.Numerics;
using Zugfish.Engine.Extensions;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

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

    #region Attacks

    public Bitboard WhiteAttacks;
    public Bitboard WhitePawnAttacks;
    public Bitboard WhiteKnightAttacks;
    public Bitboard WhiteKingAttacks;
    public Bitboard BlackAttacks;
    public Bitboard BlackPawnAttacks;
    public Bitboard BlackKnightAttacks;
    public Bitboard BlackKingAttacks;

    #endregion

    // TODO: move this out
    public readonly ulong[] RepetitionTable = new ulong[256];
    public int CurrentPly;

    /// <summary>
    /// Default constructor: sets up the standard starting position.
    /// </summary>
    public Position() : this(Constants.StartingPosition)
    {
        UpdateCombinedBitboards();
        UpdateAttacks();
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
        ZobristHash = Zobrist.ComputeHash(this);
        RepetitionTable[CurrentPly++] = ZobristHash;
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

    public bool IsInCheck()
    {
        var kingSquare = WhiteToMove ? (Square)BitOperations.TrailingZeroCount(WhiteKing) : (Square)BitOperations.TrailingZeroCount(BlackKing);
        return IsSquareAttacked(kingSquare, byWhite: !WhiteToMove);
    }

    public bool IsSquareAttacked(Square square, bool byWhite)
    {
        var enemyAttacks = byWhite ? WhiteAttacks : BlackAttacks;
        return enemyAttacks.Intersects(square);
    }

    public bool IsDrawByRepetition()
    {
        var count = 0;
        for (var i = 0; i < CurrentPly; i++)
        {
            if (RepetitionTable[i] != ZobristHash) continue;

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

    private void UpdateCombinedBitboards()
    {
        WhitePieces = WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        BlackPieces = BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        AllPieces = WhitePieces | BlackPieces;
    }

    private void UpdateAttacks()
    {
        WhiteAttacks = AttackGeneration.CalculateAttacks(this, forWhite: true);
        WhitePawnAttacks = AttackGeneration.CalculatePawnAttacks(WhitePawns, forWhite: true);
        WhiteKnightAttacks = AttackGeneration.CalculateKnightAttacks(WhiteKnights);
        WhiteKingAttacks = AttackGeneration.CalculateKingAttacks(WhiteKing);

        BlackAttacks = AttackGeneration.CalculateAttacks(this, forWhite: false);
        BlackPawnAttacks = AttackGeneration.CalculatePawnAttacks(BlackPawns, forWhite: false);
        BlackKnightAttacks = AttackGeneration.CalculateKnightAttacks(BlackKnights);
        BlackKingAttacks = AttackGeneration.CalculateKingAttacks(BlackKing);
    }
}
