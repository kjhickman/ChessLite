using System.Numerics;
using System.Runtime.CompilerServices;

namespace ChessLite.Primitives;

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

    /// <summary>
    /// Creates a bitboard with a single bit set at the specified square.
    /// </summary>
    /// <param name="square">The square to set.</param>
    /// <returns>A bitboard with only the specified square's bit set.</returns>
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
    
    /// <summary>
    /// Returns a new bitboard with the specified square's bit set.
    /// </summary>
    /// <param name="square">The square to set.</param>
    /// <returns>A new bitboard with the square's bit set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard SetSquare(Square square)
    {
        return new Bitboard(_value | Mask(square));
    }

    /// <summary>
    /// Returns a new bitboard with all bits from the specified bitboard set.
    /// </summary>
    /// <param name="bits">The bitboard containing bits to set.</param>
    /// <returns>A new bitboard with the additional bits set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard SetSquares(Bitboard bits)
    {
        return new Bitboard(_value | bits);
    }

    /// <summary>
    /// Returns a new bitboard with the specified square's bit cleared.
    /// </summary>
    /// <param name="square">The square to clear.</param>
    /// <returns>A new bitboard with the square's bit cleared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard ClearSquare(Square square)
    {
        return new Bitboard(_value & ~Mask(square));
    }

    /// <summary>
    /// Returns a new bitboard with all bits from the specified bitboard cleared.
    /// </summary>
    /// <param name="bits">The bitboard containing bits to clear.</param>
    /// <returns>A new bitboard with the specified bits cleared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard ClearSquares(Bitboard bits)
    {
        return new Bitboard(_value & ~bits);
    }

    /// <summary>
    /// Returns a new bitboard representing a piece move from one square to another.
    /// </summary>
    /// <param name="from">The source square to clear.</param>
    /// <param name="to">The destination square to set.</param>
    /// <returns>A new bitboard with the move applied.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard MoveSquare(Square from, Square to)
    {
        return ClearSquare(from).SetSquare(to);
    }

    /// <summary>
    /// Gets the index of the first (least significant) set bit as a square.
    /// </summary>
    /// <returns>The square corresponding to the first set bit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Square GetFirstSquare()
    {
        return (Square)BitOperations.TrailingZeroCount(this);
    }

    /// <summary>
    /// Determines whether this bitboard has any bits in common with another bitboard.
    /// </summary>
    /// <param name="other">The bitboard to check for intersection.</param>
    /// <returns><c>true</c> if the bitboards share at least one set bit; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(Bitboard other)
    {
        return (this & other) != 0;
    }

    /// <summary>
    /// Determines whether this bitboard has a bit set at the specified square.
    /// </summary>
    /// <param name="square">The square to check.</param>
    /// <returns><c>true</c> if the bit at the square is set; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(Square square)
    {
        return (this & Mask(square)) != 0;
    }

    /// <summary>
    /// Determines whether this bitboard has no bits in common with another bitboard.
    /// </summary>
    /// <param name="other">The bitboard to check against.</param>
    /// <returns><c>true</c> if the bitboards share no set bits; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DoesNotIntersect(Bitboard other)
    {
        return (this & other) == 0;
    }

    /// <summary>
    /// Creates a bitboard with all bits set for the specified rank (row).
    /// </summary>
    /// <param name="rank">The rank index (0-7, where 0 is the first rank).</param>
    /// <returns>A bitboard with all 8 bits of the specified rank set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when rank is not between 0 and 7.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard GetRankMask(int rank)
    {
        if (rank is < 0 or > 7)
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be between 0 and 7");

        return 0xFFUL << (rank * 8);
    }

    #endregion
}
