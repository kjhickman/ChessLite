using System.Numerics;
using System.Runtime.CompilerServices;

namespace Veloce.Models;

/// <summary>
/// Represents a 64‐bit board for chess pieces.
/// </summary>
public readonly struct Bitboard : IEquatable<Bitboard>
{
    public readonly ulong Value;

    public Bitboard(ulong value) => Value = value;

    // Implicit conversions for ease of use
    public static implicit operator ulong(Bitboard b) => b.Value;
    public static implicit operator Bitboard(ulong value) => new(value);

    // Operator overloads
    public static Bitboard operator |(Bitboard a, Bitboard b) => new(a.Value | b.Value);
    public static Bitboard operator &(Bitboard a, Bitboard b) => new(a.Value & b.Value);
    public static Bitboard operator ^(Bitboard a, Bitboard b) => new(a.Value ^ b.Value);
    public static Bitboard operator ~(Bitboard a) => new(~a.Value);
    public static bool operator ==(Bitboard left, Bitboard right) => left.Equals(right);
    public static bool operator !=(Bitboard left, Bitboard right) => !(left == right);
    public bool Equals(Bitboard other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Bitboard other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Convert.ToString((long)Value, 2).PadLeft(64, '0');

    #region Helper methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard Mask(int square) => 1UL << square;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard Mask(Square square) => 1UL << (int)square;

    /// <summary>
    /// Returns true if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => Value == 0;

    /// <summary>
    /// Returns true if no bits are set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNotEmpty() => Value != 0;

    /// <summary>
    /// Returns the number of set bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count() => BitOperations.PopCount(Value);

    /// <summary>
    /// Returns true if the given square is set in the bitboard.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(Square square) => (Value & Mask(square)) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard SetSquare(Square square)
    {
        return new Bitboard(Value | Mask(square));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard SetSquares(Bitboard bits)
    {
        return new Bitboard(Value | bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard ClearSquare(Square square)
    {
        return new Bitboard(Value & ~Mask(square));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard ClearSquares(Bitboard bits)
    {
        return new Bitboard(Value & ~bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard MoveSquare(Square from, Square to)
    {
        return ClearSquare(from).SetSquare(to);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard MoveSquares(Bitboard from, Bitboard to)
    {
        return ClearSquares(from).SetSquares(to);
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

    public static Bitboard AllOnes = ulong.MaxValue;
    #endregion
}
