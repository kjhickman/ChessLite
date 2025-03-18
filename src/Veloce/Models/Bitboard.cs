using System.Numerics;
using System.Runtime.CompilerServices;

namespace Veloce.Models;

/// <summary>
/// Represents a 64‐bit board for chess pieces.
/// </summary>
public readonly struct Bitboard : IEquatable<Bitboard>
{
    private readonly ulong _value;

    private Bitboard(ulong value) => _value = value;

    // Implicit conversions for ease of use
    public static implicit operator ulong(Bitboard b) => b._value;
    public static implicit operator Bitboard(ulong value) => new(value);

    // Operator overloads
    public static Bitboard operator |(Bitboard a, Bitboard b) => new(a._value | b._value);
    public static Bitboard operator &(Bitboard a, Bitboard b) => new(a._value & b._value);
    public static Bitboard operator ^(Bitboard a, Bitboard b) => new(a._value ^ b._value);
    public static Bitboard operator ~(Bitboard a) => new(~a._value);
    public static bool operator ==(Bitboard left, Bitboard right) => left.Equals(right);
    public static bool operator !=(Bitboard left, Bitboard right) => !(left == right);
    public bool Equals(Bitboard other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Bitboard other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => Convert.ToString((long)_value, 2).PadLeft(64, '0');

    #region Helper methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard Mask(Square square) => 1UL << (int)square;

    /// <summary>
    /// Returns true if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => _value == 0;

    /// <summary>
    /// Returns true if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNotEmpty() => _value != 0;

    /// <summary>
    /// Returns the number of set bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count() => BitOperations.PopCount(_value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard SetSquare(Square square)
    {
        return new Bitboard(_value | Mask(square));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard SetSquares(Bitboard bits)
    {
        return new Bitboard(_value | bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard ClearSquare(Square square)
    {
        return new Bitboard(_value & ~Mask(square));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard ClearSquares(Bitboard bits)
    {
        return new Bitboard(_value & ~bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard MoveSquare(Square from, Square to)
    {
        return ClearSquare(from).SetSquare(to);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Square GetFirstSquare()
    {
        return (Square)BitOperations.TrailingZeroCount(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(Bitboard other)
    {
        return (this & other) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(Square square)
    {
        return (this & Mask(square)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoesNotIntersect(Bitboard other)
    {
        return (this & other) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard GetRankMask(int rank)
    {
        if (rank is < 0 or > 7)
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be between 0 and 7");

        return 0xFFUL << (rank * 8);
    }

    #endregion
}
