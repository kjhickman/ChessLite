namespace Zugfish.Engine;

/// <summary>
/// Represents a 64‐bit board for chess pieces.
/// </summary>
public readonly struct Bitboard : IEquatable<Bitboard>
{
    public readonly ulong Value;

    public Bitboard(ulong value) => Value = value;

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
}
