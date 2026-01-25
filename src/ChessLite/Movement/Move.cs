using ChessLite.Primitives;

namespace ChessLite.Movement;

/// <summary>
/// Represents a chess move with all associated metadata packed into a single integer for efficiency.
/// </summary>
/// <remarks>
/// This struct uses bit packing to store all move information in a single 32-bit integer,
/// making it efficient for move generation and storage in transposition tables.
/// </remarks>
public readonly struct Move : IEquatable<Move>
{
    /// <summary>
    /// The internal packed representation of the move.
    ///
    /// Bit layout (from least-significant to most-significant bits):
    /// - Bits 0-5:   Source square index (0 to 63)
    /// - Bits 6-11:  Target square index (0 to 63)
    /// - Bits 12-15: Promoted piece type (4 bit flags, representing the piece type)
    /// - Bits 16-19: Piece type (0-11)
    /// - Bits 20-23: Captured piece type (0-11)
    /// - Bit 24:     Is capture
    /// - Bits 25-27: Move type
    /// </summary>
    private readonly int _packed;

    private const int TargetSquareOffset = 6;
    private const int PromotedPieceTypeOffset = 12;
    private const int PieceTypeOffset = 16;
    private const int CapturedPieceTypeOffset = 20;
    private const int IsCaptureOffset = 24;
    private const int MoveTypeOffset = 25;

    /// <summary>
    /// Gets the source square of the move.
    /// </summary>
    public Square From => (Square)(_packed & 0x3F);

    /// <summary>
    /// Gets the target square of the move.
    /// </summary>
    public Square To => (Square)((_packed >> TargetSquareOffset) & 0x3F);

    /// <summary>
    /// Gets the piece type that a pawn promotes to. Returns <see cref="Primitives.PromotedPieceType.None"/> if not a promotion.
    /// </summary>
    public PromotedPieceType PromotedPieceType => (PromotedPieceType)((_packed >> PromotedPieceTypeOffset) & 0xF);

    /// <summary>
    /// Gets the type of piece being moved.
    /// </summary>
    public PieceType PieceType => (PieceType)((_packed >> PieceTypeOffset) & 0xF);

    /// <summary>
    /// Gets the type of piece being captured. Returns <see cref="Primitives.PieceType.None"/> if not a capture.
    /// </summary>
    public PieceType CapturedPieceType => (PieceType)((_packed >> CapturedPieceTypeOffset) & 0xF);

    /// <summary>
    /// Gets a value indicating whether this move captures an opponent's piece.
    /// </summary>
    public bool IsCapture => (_packed & (1 << IsCaptureOffset)) != 0;

    /// <summary>
    /// Gets the special move type (castling, en passant, double pawn push, or none).
    /// </summary>
    public SpecialMoveType SpecialMoveType => (SpecialMoveType)((_packed >> MoveTypeOffset) & 0x7);

    /// <summary>
    /// Initializes a new instance of the <see cref="Move"/> struct with all move details.
    /// </summary>
    /// <param name="from">The source square.</param>
    /// <param name="to">The target square.</param>
    /// <param name="promotedPieceType">The piece type to promote to, or <see cref="PromotedPieceType.None"/>.</param>
    /// <param name="pieceType">The type of piece being moved.</param>
    /// <param name="capturedPieceType">The type of piece being captured, or <see cref="PieceType.None"/>.</param>
    /// <param name="isCapture">Whether this move captures a piece.</param>
    /// <param name="moveType">The special move type, if any.</param>
    public Move(Square from, Square to, PromotedPieceType promotedPieceType, PieceType pieceType,
        PieceType capturedPieceType, bool isCapture, SpecialMoveType moveType)
    {
        _packed = (int)from | ((int)to << TargetSquareOffset) | ((int)promotedPieceType << PromotedPieceTypeOffset) |
                  ((int)pieceType << PieceTypeOffset) | ((int)capturedPieceType << CapturedPieceTypeOffset) |
                  (isCapture ? 1 << IsCaptureOffset : 0) | ((int)moveType << MoveTypeOffset);
    }

    /// <summary>
    /// Determines whether two moves are equal.
    /// </summary>
    /// <param name="left">The first move to compare.</param>
    /// <param name="right">The second move to compare.</param>
    /// <returns><c>true</c> if the moves are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Move left, Move right) => left.Equals(right);

    /// <summary>
    /// Determines whether two moves are not equal.
    /// </summary>
    /// <param name="left">The first move to compare.</param>
    /// <param name="right">The second move to compare.</param>
    /// <returns><c>true</c> if the moves are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Move left, Move right) => !(left == right);

    /// <inheritdoc />
    public bool Equals(Move other) => _packed == other._packed;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Move other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _packed;

    /// <summary>
    /// Returns the move in UCI (Universal Chess Interface) notation.
    /// </summary>
    /// <returns>A string in the format "e2e4" or "e7e8q" for promotions.</returns>
    public override string ToString()
    {
        var promotion = PromotedPieceType switch
        {
            PromotedPieceType.None => string.Empty,
            PromotedPieceType.Knight => "k",
            PromotedPieceType.Bishop => "b",
            PromotedPieceType.Rook => "r",
            PromotedPieceType.Queen => "q",
            _ => throw new ArgumentOutOfRangeException(),
        };

        return $"{From}{To}{promotion}";
    }

    /// <summary>
    /// Gets a null move, used for null move pruning in search algorithms.
    /// </summary>
    public static Move NullMove => new(Square.None, Square.None, PromotedPieceType.None, PieceType.None, PieceType.None, false, SpecialMoveType.None);

    /// <summary>
    /// Creates a quiet (non-capturing) move.
    /// </summary>
    /// <param name="from">The source square.</param>
    /// <param name="to">The target square.</param>
    /// <param name="pieceType">The type of piece being moved.</param>
    /// <returns>A new quiet move.</returns>
    public static Move CreateQuiet(Square from, Square to, PieceType pieceType)
    {
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.None);
    }

    /// <summary>
    /// Creates a capturing move.
    /// </summary>
    /// <param name="from">The source square.</param>
    /// <param name="to">The target square.</param>
    /// <param name="pieceType">The type of piece being moved.</param>
    /// <param name="capturedPieceType">The type of piece being captured.</param>
    /// <returns>A new capture move.</returns>
    public static Move CreateCapture(Square from, Square to, PieceType pieceType, PieceType capturedPieceType)
    {
        return new Move(from, to, PromotedPieceType.None, pieceType, capturedPieceType, true, SpecialMoveType.None);
    }

    /// <summary>
    /// Creates a pawn promotion move without capture.
    /// </summary>
    /// <param name="from">The source square.</param>
    /// <param name="to">The target square.</param>
    /// <param name="pieceType">The type of pawn being promoted.</param>
    /// <param name="promotedPieceType">The piece type to promote to.</param>
    /// <returns>A new promotion move.</returns>
    public static Move CreatePromotion(Square from, Square to, PieceType pieceType, PromotedPieceType promotedPieceType)
    {
        return new Move(from, to, promotedPieceType, pieceType, PieceType.None, false, SpecialMoveType.None);
    }

    /// <summary>
    /// Creates a pawn promotion move with capture.
    /// </summary>
    /// <param name="from">The source square.</param>
    /// <param name="to">The target square.</param>
    /// <param name="pieceType">The type of pawn being promoted.</param>
    /// <param name="capturedPieceType">The type of piece being captured.</param>
    /// <param name="promotedPieceType">The piece type to promote to.</param>
    /// <returns>A new promotion capture move.</returns>
    public static Move CreatePromotion(Square from, Square to, PieceType pieceType, PieceType capturedPieceType, PromotedPieceType promotedPieceType)
    {
        return new Move(from, to, promotedPieceType, pieceType, capturedPieceType, true, SpecialMoveType.None);
    }

    /// <summary>
    /// Creates an en passant capture move.
    /// </summary>
    /// <param name="from">The source square of the capturing pawn.</param>
    /// <param name="to">The target square (where the pawn lands).</param>
    /// <param name="isWhite">Whether the capturing pawn is white.</param>
    /// <returns>A new en passant move.</returns>
    public static Move CreateEnPassant(Square from, Square to, bool isWhite)
    {
        var pieceType = isWhite ? PieceType.WhitePawn : PieceType.BlackPawn;
        var capturedPieceType = isWhite ? PieceType.BlackPawn : PieceType.WhitePawn;
        return new Move(from, to, PromotedPieceType.None, pieceType, capturedPieceType, true, SpecialMoveType.EnPassant);
    }

    /// <summary>
    /// Creates a kingside (short) castling move.
    /// </summary>
    /// <param name="isWhite">Whether the castling side is white.</param>
    /// <returns>A new short castle move.</returns>
    public static Move CreateShortCastle(bool isWhite)
    {
        var pieceType = isWhite ? PieceType.WhiteKing : PieceType.BlackKing;
        var from = isWhite ? Square.e1 : Square.e8;
        var to = isWhite ? Square.g1 : Square.g8;
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.ShortCastle);
    }

    /// <summary>
    /// Creates a queenside (long) castling move.
    /// </summary>
    /// <param name="isWhite">Whether the castling side is white.</param>
    /// <returns>A new long castle move.</returns>
    public static Move CreateLongCastle(bool isWhite)
    {
        var pieceType = isWhite ? PieceType.WhiteKing : PieceType.BlackKing;
        var from = isWhite ? Square.e1 : Square.e8;
        var to = isWhite ? Square.c1 : Square.c8;
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.LongCastle);
    }

    /// <summary>
    /// Creates a double pawn push move (pawn moving two squares from starting position).
    /// </summary>
    /// <param name="from">The source square.</param>
    /// <param name="to">The target square.</param>
    /// <param name="pieceType">The type of pawn being moved.</param>
    /// <returns>A new double pawn push move.</returns>
    public static Move CreateDoublePawnPush(Square from, Square to, PieceType pieceType)
    {
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.DoublePawnPush);
    }
}
