using System.Runtime.CompilerServices;

namespace Zugfish.Engine.Models;

public readonly struct TranspositionEntry : IEquatable<TranspositionEntry>
{
    /// The internal packed representation of the transposition entry.
    ///
    /// Bit layout (from least-significant to most-significant bits):
    /// - Bits 0-15:  16-bit move (compact move representation)
    /// - Bits 16-31: 16-bit hash verification
    /// - Bits 32-47: 16-bit evaluation
    /// - Bits 48-63: 16-bit score
    /// - Bits 64-71: 8-bit depth
    /// - Bits 72-79: 8-bit (6-bit generation + 2-bit node type)
    /// Total: 80 bits / 10 bytes. Fits 3 into a 32-byte cluster
    private readonly ulong _packedLower;
    private readonly ushort _packedUpper;

    // Lower offsets
    private const int HashVerificationOffset = 16;
    private const int EvaluationOffset = 32;
    private const int ScoreOffset = 48;

    // Upper offsets
    private const int GenerationAndNodeTypeOffset = 8;

    public CompactMove BestMove => (ushort)(_packedLower & 0xFFFF);
    public ushort HashVerification => (ushort)((_packedLower >> HashVerificationOffset) & 0xFFFF);
    public short Evaluation => (short)((_packedLower >> EvaluationOffset) & 0xFFFF);
    public short Score => (short)((_packedLower >> ScoreOffset) & 0xFFFF);
    public byte Depth => (byte)(_packedUpper & 0xFF);
    public byte Generation => (byte)((_packedUpper >> GenerationAndNodeTypeOffset) & 0x3F);
    public TranspositionNodeType NodeType => (TranspositionNodeType)((_packedUpper >> (GenerationAndNodeTypeOffset + 6)) & 0x3);

    public TranspositionEntry(ushort move, ushort hashVerification, short evaluation, short score,
        byte depth, byte generation, TranspositionNodeType nodeType)
    {
        _packedLower = move |
                      ((ulong)hashVerification << HashVerificationOffset) |
                      ((ulong)(ushort)evaluation << EvaluationOffset) |
                      ((ulong)(ushort)score << ScoreOffset);

        _packedUpper = (ushort)(depth |
                      (generation << GenerationAndNodeTypeOffset) |
                      ((int)nodeType << (GenerationAndNodeTypeOffset + 6)));
    }

    public static bool operator ==(TranspositionEntry left, TranspositionEntry right) => left.Equals(right);
    public static bool operator !=(TranspositionEntry left, TranspositionEntry right) => !(left == right);
    public bool Equals(TranspositionEntry other) => _packedLower == other._packedLower && _packedUpper == other._packedUpper;
    public override bool Equals(object? obj) => obj is TranspositionEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_packedLower, _packedUpper);

    /// <summary>
    /// Determines if this entry is better to replace than another entry
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBetterThan(in TranspositionEntry other, byte currentGeneration)
    {
        // Always prefer current generation entries
        var thisGen = Generation;
        var otherGen = other.Generation;

        var thisIsCurrent = Math.Abs(thisGen - currentGeneration) < 3;
        var otherIsCurrent = Math.Abs(otherGen - currentGeneration) < 3;

        if (thisIsCurrent != otherIsCurrent)
            return thisIsCurrent;

        // Prefer deeper searches
        if (Depth != other.Depth)
            return Depth > other.Depth;

        // Prefer exact bounds over non-exact bounds
        return NodeType == TranspositionNodeType.Exact && other.NodeType != TranspositionNodeType.Exact;
    }
}
