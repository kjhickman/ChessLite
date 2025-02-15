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
        for (var i = 0; i < _table.Length; i++)
        {
            _table[i] = default;
        }
    }

    public void Store(ulong key, int depth, int score, NodeType flag, Move bestMove)
    {
        var index = (int)(key & (ulong)_mask);
        var startIndex = index;
        var candidateIndex = -1;

        do
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

            // Record this slot as a candidate for replacement if the stored depth is lower.
            if (_table[index].Depth < depth)
                candidateIndex = index;

            index = (index + 1) & _mask;
        } while (index != startIndex);

        // If no empty slot or matching key was found and we have a candidate, replace it.
        if (candidateIndex != -1)
        {
            _table[candidateIndex] = new TranspositionEntry
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

        while (true)
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
    }
}
