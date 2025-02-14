using System.Runtime.CompilerServices;

namespace Zugfish.Engine.Models;

public readonly struct Move : IEquatable<Move>
{
    /// <summary>
    /// The internal packed representation of the move.
    ///
    /// Bit layout (from least-significant to most-significant bits):
    /// - Bits 0-5:   Source square index (0 to 63)
    /// - Bits 6-11:  Destination square index (0 to 63)
    /// - Bits 12-15: Move flags (4 bits, representing the move type)
    /// </summary>
    private readonly ushort _packed;
    
    public Move(int from, int to, MoveType moveType)
    {
        if ((from | to) >> 6 != 0) // Ensure from and to are in 0..63
            throw new ArgumentOutOfRangeException();

        var typeValue = (ushort)((byte)moveType & 0xF);
        _packed = (ushort)((from & 0x3F) | ((to & 0x3F) << 6) | (typeValue << 12));
    }

    public int From => _packed & 0x3F;
    public int To => (_packed >> 6) & 0x3F;
    public MoveType Type => (MoveType)(_packed >> 12);
    public override string ToString() => $"{(char)('a' + (From & 7))}{(char)('1' + (From >> 3))}{(char)('a' + (To & 7))}{(char)('1' + (To >> 3))}";
    public bool Equals(Move other) => _packed == other._packed;
    public override bool Equals(object? obj) => obj is Move other && Equals(other);
    public override int GetHashCode() => _packed;

    public static bool operator ==(Move left, Move right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Move left, Move right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns true if the move is a promotion move.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPromotion()
    {
        return Type is
            MoveType.PromoteToQueen or
            MoveType.PromoteToRook or
            MoveType.PromoteToBishop or
            MoveType.PromoteToKnight;
    }

    /// <summary>
    /// Returns true if the move is a capture move (including en passant).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCapture() => Type is MoveType.Capture or MoveType.EnPassant;

    /// <summary>
    /// Returns true if the move is a castling move.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCastling() => Type == MoveType.Castling;

    /// <summary>
    /// Returns true if the move is a quiet (non-capturing, non-promotional) move.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsQuiet() => Type == MoveType.Quiet;

    /// <summary>
    /// Returns a new move with the same from/to but a different move type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Move WithType(MoveType newType) => new(From, To, newType);
}
