using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public enum NodeType
{
    Exact,
    Alpha,
    Beta
}

public struct TranspositionEntry
{
    // A key of 0 is assumed to mean the slot is unused.
    public ulong Hash;
    public int Depth;
    public int Score;
    public NodeType Flag;
    public Move BestMove;
}

// TODO: create a fake transposition table for benchmarking
public class TranspositionTable
{
    private readonly TranspositionEntry[] _table;
    private readonly int _mask;

    public TranspositionTable(int size)
    {
        if ((size & (size - 1)) != 0)
            throw new ArgumentException("Size must be a power of 2", nameof(size));

        _table = new TranspositionEntry[size];
        _mask = size - 1;
    }

    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
    }

    public void Store(ulong hash, int depth, int score, NodeType flag, Move bestMove)
    {
        var index = (int)(hash & (ulong)_mask);
        var entry = _table[index];

        if (_table[index].Hash == 0 || depth >= entry.Depth)
        {
            _table[index] = new TranspositionEntry
            {
                Hash = hash,
                Depth = depth,
                Score = score,
                Flag = flag,
                BestMove = bestMove
            };
        }
    }

    public bool TryGet(ulong hash, out TranspositionEntry entry)
    {
        var index = (int)(hash & (ulong)_mask);
        if (_table[index].Hash == hash)
        {
            entry = _table[index];
            return true;
        }

        entry = default;
        return false;
    }
}
