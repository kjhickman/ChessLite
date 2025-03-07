namespace Zugfish.Engine.Models;

public struct TranspositionEntryOld
{
    // A key of 0 is assumed to mean the slot is unused.
    public ulong Hash;
    public int Depth;
    public int Score;
    public TranspositionNodeType NodeType;
    public Move BestMove;
}

// TODO: Figure out how to mitigate collisions / false verification
