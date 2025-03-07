using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public class TranspositionTableOld
{
    private readonly TranspositionEntryOld[] _table;
    private readonly int _mask;

    public TranspositionTableOld(int size)
    {
        if ((size & (size - 1)) != 0)
            throw new ArgumentException("Size must be a power of 2", nameof(size));

        _table = new TranspositionEntryOld[size];
        _mask = size - 1;
    }

    public void Store(ulong hash, int depth, int score, TranspositionNodeType flag, Move bestMove)
    {
        var index = (int)(hash & (ulong)_mask);
        var entry = _table[index];

        if (_table[index].Hash == 0 || depth >= entry.Depth)
        {
            _table[index] = new TranspositionEntryOld
            {
                Hash = hash,
                Depth = depth,
                Score = score,
                NodeType = flag,
                BestMove = bestMove
            };
        }
    }

    public bool TryGet(ulong hash, out TranspositionEntryOld entryOld)
    {
        var index = (int)(hash & (ulong)_mask);
        if (_table[index].Hash == hash)
        {
            entryOld = _table[index];
            return true;
        }

        entryOld = default;
        return false;
    }

    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
    }
}
