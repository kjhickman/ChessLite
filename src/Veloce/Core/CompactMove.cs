namespace Veloce.Core;

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

    /// <summary>
    /// Finds a move in the move list that matches the given CompactMove
    /// </summary>
    public Move FindMatchingMove(Span<Move> moveList, int moveCount)
    {
        // Early exit for null move
        if (_packed == 0) return Move.NullMove;

        // Extract from and to squares
        var from = (Square)(_packed & 0x3F);
        var to = (Square)((_packed >> 6) & 0x3F);
        var promotedType = (PromotedPieceType)((_packed >> 12) & 0xF);

        // Find matching move in the list
        for (var i = 0; i < moveCount; i++)
        {
            if (moveList[i].From == from && moveList[i].To == to)
            {
                // For promotions, check the promotion type too
                if (promotedType != PromotedPieceType.None)
                {
                    if (moveList[i].PromotedPieceType == promotedType)
                        return moveList[i];
                }
                else
                {
                    return moveList[i];
                }
            }
        }

        // If no match is found, return NullMove
        return Move.NullMove;
    }
}
