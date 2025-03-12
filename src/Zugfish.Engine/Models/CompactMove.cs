namespace Zugfish.Engine.Models;

public readonly struct CompactMove : IEquatable<CompactMove>
{
    private readonly ushort _packed;

    public static implicit operator CompactMove(ushort value) => new(value);
    public static implicit operator ushort(CompactMove value) => value._packed;
    public static bool operator ==(CompactMove left, CompactMove right) => left.Equals(right);
    public static bool operator !=(CompactMove left, CompactMove right) => !(left == right);
    public bool Equals(CompactMove other) => _packed == other._packed;
    public override bool Equals(object? obj) => obj is CompactMove other && Equals(other);
    public override int GetHashCode() => _packed;
    public CompactMove(ushort packed) => _packed = packed;
}
