using Zugfish.Engine.Models;

namespace Zugfish.Engine;

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

    public void Store(ulong hash, int depth, int score, TranspositionNodeType flag, Move bestMove)
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
                NodeType = flag,
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

    /// <summary>
    /// Returns an estimate of the transposition table occupancy as a permill value (0-1000).
    /// </summary>
    public int GetOccupancy()
    {
        const int sampleSize = 1000;
        var occupied = 0;

        for (var i = 0; i < sampleSize; i++)
        {
            if (_table[i].Hash != 0)
            {
                occupied++;
            }
        }

        return occupied;
    }

    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
    }
}
