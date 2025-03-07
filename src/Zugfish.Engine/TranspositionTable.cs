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
    public ulong Key;
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

    public void Store(ulong key, int depth, int score, NodeType flag, Move bestMove)
    {
        var index = (int)(key & (ulong)_mask);
        var bestReplaceIndex = -1;
        var bestReplaceDepth = int.MaxValue;

        // Only probe up to MaxProbes slots, consistent with TryGet
        for (int i = 0; i < 4; i++)
        {
            // If the slot is empty, we can store the new entry here.
            if (_table[index].Key == 0)
            {
                _table[index] = new TranspositionEntry
                {
                    Key = key,
                    Depth = depth,
                    Score = score,
                    Flag = flag,
                    BestMove = bestMove
                };
                return;
            }

            // If the slot already contains the same key, update it if the new depth is better.
            if (_table[index].Key == key)
            {
                if (depth >= _table[index].Depth)
                {
                    _table[index] = new TranspositionEntry
                    {
                        Key = key,
                        Depth = depth,
                        Score = score,
                        Flag = flag,
                        BestMove = bestMove
                    };
                }
                return;
            }

            // Track the slot with the lowest depth as a replacement candidate
            if (_table[index].Depth < bestReplaceDepth)
            {
                bestReplaceDepth = _table[index].Depth;
                bestReplaceIndex = index;
            }

            index = (index + 1) & _mask;
        }

        // If no empty slot or matching key was found within MaxProbes,
        // replace the entry with the lowest depth if the new depth is higher
        if (bestReplaceIndex != -1 && depth > bestReplaceDepth)
        {
            _table[bestReplaceIndex] = new TranspositionEntry
            {
                Key = key,
                Depth = depth,
                Score = score,
                Flag = flag,
                BestMove = bestMove
            };
        }
    }

    public bool TryGet(ulong key, out TranspositionEntry entry)
    {
        var index = (int)(key & (ulong)_mask);
        var i = 0;

        while (i++ < 4)
        {
            if (_table[index].Key == key)
            {
                entry = _table[index];
                return true;
            }
            if (_table[index].Key == 0)
            {
                entry = default;
                return false;
            }
            index = (index + 1) & _mask;
        }

        entry = default;
        return false;
    }

    public bool TryGet2(ulong key, out TranspositionEntry entry)
    {
        var index = (int)(key & (ulong)_mask);
        var startIndex = index;

        do
        {
            if (_table[index].Key == key)
            {
                entry = _table[index];
                return true;
            }
            if (_table[index].Key == 0)
            {
                entry = default;
                return false;
            }
            index = (index + 1) & _mask;
        } while (index != startIndex);

        entry = default;
        return false;
    }
}
