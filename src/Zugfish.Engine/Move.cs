namespace Zugfish.Engine;

public readonly struct Move : IEquatable<Move>
{
    // Packed Move: bits 0-5 = from, bits 6-11 = to, bits 12-15 = flags.
    private readonly ushort _packed;
    
    public Move(int from, int to, MoveType moveType)
    {
        if ((from | to) >> 6 != 0) // Ensure from and to are in 0..63
            throw new ArgumentOutOfRangeException();

        var typeValue = (ushort)((byte)moveType & 0xF);
        _packed = (ushort)((from & 0x3F) | ((to & 0x3F) << 6) | (typeValue << 12));
    }

    // TODO: public Move(ReadOnlySpan<char> uciMove)

    public int From => _packed & 0x3F;
    public int To => (_packed >> 6) & 0x3F;
    public int Flag => (_packed >> 12) & 0xF;
    public override string ToString() => $"From: {From}, To: {To}, Flag: {Flag}";
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
    
    public static MoveType GetMoveType(int flags) => (MoveType)(flags & 0x7);
}
