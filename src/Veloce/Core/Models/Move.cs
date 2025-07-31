namespace Veloce.Core.Models;

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

    public Square From => (Square)(_packed & 0x3F);
    public Square To => (Square)((_packed >> TargetSquareOffset) & 0x3F);
    public PromotedPieceType PromotedPieceType => (PromotedPieceType)((_packed >> PromotedPieceTypeOffset) & 0xF);
    public PieceType PieceType => (PieceType)((_packed >> PieceTypeOffset) & 0xF);
    public PieceType CapturedPieceType => (PieceType)((_packed >> CapturedPieceTypeOffset) & 0xF);
    public bool IsCapture => (_packed & (1 << IsCaptureOffset)) != 0;
    public SpecialMoveType SpecialMoveType => (SpecialMoveType)((_packed >> MoveTypeOffset) & 0x7);

    public Move(Square from, Square to, PromotedPieceType promotedPieceType, PieceType pieceType,
        PieceType capturedPieceType, bool isCapture, SpecialMoveType moveType)
    {
        _packed = (int)from | ((int)to << TargetSquareOffset) | ((int)promotedPieceType << PromotedPieceTypeOffset) |
                  ((int)pieceType << PieceTypeOffset) | ((int)capturedPieceType << CapturedPieceTypeOffset) |
                  (isCapture ? 1 << IsCaptureOffset : 0) | ((int)moveType << MoveTypeOffset);
    }

    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !(left == right);
    public bool Equals(Move other) => _packed == other._packed;
    public override bool Equals(object? obj) => obj is Move other && Equals(other);
    public override int GetHashCode() => _packed;

    public override string ToString()
    {
        var promotion = PromotedPieceType switch
        {
            PromotedPieceType.None => string.Empty,
            PromotedPieceType.Knight => "k",
            PromotedPieceType.Bishop => "b",
            PromotedPieceType.Rook => "r",
            PromotedPieceType.Queen => "q",
            _ => throw new ArgumentOutOfRangeException()
        };

        return $"{From}{To}{promotion}";
    }

    public static Move NullMove => new(Square.None, Square.None, PromotedPieceType.None, PieceType.None, PieceType.None, false, SpecialMoveType.None);

    public static Move CreateQuiet(Square from, Square to, PieceType pieceType)
    {
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.None);
    }

    public static Move CreateCapture(Square from, Square to, PieceType pieceType, PieceType capturedPieceType)
    {
        return new Move(from, to, PromotedPieceType.None, pieceType, capturedPieceType, true, SpecialMoveType.None);
    }

    public static Move CreatePromotion(Square from, Square to, PieceType pieceType, PromotedPieceType promotedPieceType)
    {
        return new Move(from, to, promotedPieceType, pieceType, PieceType.None, false, SpecialMoveType.None);
    }

    public static Move CreatePromotion(Square from, Square to, PieceType pieceType, PieceType capturedPieceType, PromotedPieceType promotedPieceType)
    {
        return new Move(from, to, promotedPieceType, pieceType, capturedPieceType, true, SpecialMoveType.None);
    }

    public static Move CreateEnPassant(Square from, Square to, bool isWhite)
    {
        var pieceType = isWhite ? PieceType.WhitePawn : PieceType.BlackPawn;
        var capturedPieceType = isWhite ? PieceType.BlackPawn : PieceType.WhitePawn;
        return new Move(from, to, PromotedPieceType.None, pieceType, capturedPieceType, true, SpecialMoveType.EnPassant);
    }

    public static Move CreateShortCastle(bool isWhite)
    {
        var pieceType = isWhite ? PieceType.WhiteKing : PieceType.BlackKing;
        var from = isWhite ? Square.e1 : Square.e8;
        var to = isWhite ? Square.g1 : Square.g8;
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.ShortCastle);
    }

    public static Move CreateLongCastle(bool isWhite)
    {
        var pieceType = isWhite ? PieceType.WhiteKing : PieceType.BlackKing;
        var from = isWhite ? Square.e1 : Square.e8;
        var to = isWhite ? Square.c1 : Square.c8;
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.LongCastle);
    }

    public static Move CreateDoublePawnPush(Square from, Square to, PieceType pieceType)
    {
        return new Move(from, to, PromotedPieceType.None, pieceType, PieceType.None, false, SpecialMoveType.DoublePawnPush);
    }
}
