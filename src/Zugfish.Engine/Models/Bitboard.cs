using System.Numerics;
using System.Runtime.CompilerServices;

namespace Zugfish.Engine.Models;

/// <summary>
/// Represents a 64‐bit board for chess pieces.
/// </summary>
public readonly struct Bitboard : IEquatable<Bitboard>
{
    public readonly ulong Value;

    public Bitboard(ulong value) => Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard Mask(int square) => 1UL << square;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard Mask(Square square) => 1UL << (int)square;

    // Implicit conversions for ease of use.
    public static implicit operator ulong(Bitboard b) => b.Value;
    public static implicit operator Bitboard(ulong value) => new(value);

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
    #endregion
}

public static class BitboardExtensions
{
    // public static void SetBit(this ref Bitboard board, int square)
    // {
    //     board = new Bitboard(board.Value | (1UL << square));
    // }

    public static void SetBit(this ref Bitboard board, Square square)
    {
        board = new Bitboard(board.Value | Bitboard.Mask(square));
    }

    public static void SetBits(this ref Bitboard board, Bitboard bits)
    {
        board = new Bitboard(board.Value | bits);
    }

    // public static void ClearBit(this ref Bitboard board, int square)
    // {
    //     board = new Bitboard(board.Value & ~Bitboard.Mask(square));
    // }

    public static void ClearBit(this ref Bitboard board, Square square)
    {
        board = new Bitboard(board.Value & ~Bitboard.Mask(square));
    }

    public static void ClearBits(this ref Bitboard board, Bitboard bits)
    {
        board = new Bitboard(board.Value & ~bits);
    }
}
