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

    public Move(ReadOnlySpan<char> uciMove)
    {
        if (uciMove.Length is < 4 or > 5)
            throw new ArgumentException("Invalid UCI move format.", nameof(uciMove));

        var from = SquareIndexFromUci(uciMove[0], uciMove[1]);
        var to = SquareIndexFromUci(uciMove[2], uciMove[3]);

        if ((from | to) >> 6 != 0) // Ensure indices are valid
            throw new ArgumentOutOfRangeException(nameof(uciMove), "Square index out of range.");

        // Default to a quiet move
        var moveType = MoveType.Quiet;

        // If the move has a 5th character (promotion), determine its type
        if (uciMove.Length == 5)
        {
            moveType = uciMove[4] switch
            {
                'q' => MoveType.PromoteToQueen,
                'r' => MoveType.PromoteToRook,
                'b' => MoveType.PromoteToBishop,
                'n' => MoveType.PromoteToKnight,
                _ => throw new ArgumentException("Invalid promotion piece.", nameof(uciMove))
            };
        }

        var typeValue = (ushort)((byte)moveType & 0xF);
        _packed = (ushort)((from & 0x3F) | ((to & 0x3F) << 6) | (typeValue << 12));
    }

    // Helper method to convert UCI square notation (e.g., "e2") to bitboard index
    private static int SquareIndexFromUci(char file, char rank)
    {
        if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            throw new ArgumentException("Invalid UCI square.", nameof(file));

        return (rank - '1') * 8 + (file - 'a');
    }

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
