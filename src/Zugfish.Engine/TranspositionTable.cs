using System.Numerics;
using System.Runtime.CompilerServices;
using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public class TranspositionTable
{
    private TranspositionEntryCluster[] _table = null!;
    private byte _generation;
    private int _clusterCount;
    private int _clusterMask;

    public TranspositionTable(int megabytes)
    {
        SetSize(megabytes);
    }

    /// <summary>
    /// Resizes the transposition table to the specified size in MB and clears it.
    /// </summary>
    public void SetSize(int megabytes)
    {
        // Calculate total bytes and cluster count
        var maxPossibleClusters = megabytes * 32768;

        // Find the largest power of 2 less than or equal to maxPossibleClusters using bit manipulation
        var power = BitOperations.Log2((ulong)maxPossibleClusters);

        _clusterCount = 1 << power; // 2^power
        _clusterMask = _clusterCount - 1;
        _table = new TranspositionEntryCluster[_clusterCount];
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

    /// <summary>
    /// Clear the table and increment the generation counter
    /// </summary>
    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
        _generation = 0;
    }

    /// <summary>
    /// Start a new search generation
    /// </summary>
    public void NewSearch()
    {
        _generation = (byte)((_generation + 1) & 0x3F); // Keep within 6 bits
    }

    /// <summary>
    /// Calculate the cluster index for a given hash key
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetClusterIndex(ulong key)
    {
        return (int)(key & (ulong)_clusterMask);
    }

    /// <summary>
    /// Extract the verification bits from a hash key
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetVerification(ulong key)
    {
        return (ushort)(key >> 48);
    }

    /// <summary>
    /// Store a position in the transposition table
    /// </summary>
    public void Store(ulong key, ushort move, short score, short staticEval, byte depth, TranspositionNodeType nodeType)
    {
        var index = GetClusterIndex(key);
        var verification = GetVerification(key);

        ref var cluster = ref _table[index];

        // Find the best entry to replace
        ref var replace = ref cluster.Entry1;

        // Check if we can replace an entry with the same position
        if (cluster.Entry1.HashVerification == verification)
        {
            replace = ref cluster.Entry1;
        }
        else if (cluster.Entry2.HashVerification == verification)
        {
            replace = ref cluster.Entry2;
        }
        else if (cluster.Entry3.HashVerification == verification)
        {
            replace = ref cluster.Entry3;
        }
        else
        {
            // No matching position, find the entry to replace based on policy
            if (cluster.Entry2.IsBetterThan(cluster.Entry1, _generation))
            {
                if (cluster.Entry3.IsBetterThan(cluster.Entry2, _generation))
                {
                    replace = ref cluster.Entry1;
                }
                else
                {
                    replace = ref cluster.Entry3;
                }
            }
            else
            {
                if (cluster.Entry3.IsBetterThan(cluster.Entry1, _generation))
                {
                    replace = ref cluster.Entry2;
                }
                else
                {
                    replace = ref cluster.Entry3;
                }
            }
        }

        // Update the entry
        // replace.HashVerification = verification;
        // replace.BestMove = move;
        // replace.Score = score;
        // replace.Evaluation = staticEval;
        // replace.Depth = depth;
        // replace.NodeType = nodeType;
        // replace.Generation = _generation;

        replace = new TranspositionEntry(); // TODO: this
    }

    /// <summary>
    /// Probe the transposition table for a position
    /// </summary>
    /// <returns>True if the position was found</returns>
    public bool Probe(ulong key, out CompactMove move, out short score, out short staticEval, out byte depth,
        out TranspositionNodeType bound)
    {
        var index = GetClusterIndex(key);
        var verify = GetVerification(key);

        ref var cluster = ref _table[index];

        // Check each entry in the cluster
        if (cluster.Entry1.HashVerification == verify)
        {
            ref var entry = ref cluster.Entry1;
            move = entry.BestMove;
            score = entry.Score;
            staticEval = entry.Evaluation;
            depth = entry.Depth;
            bound = entry.NodeType;
            return true;
        }

        if (cluster.Entry2.HashVerification == verify)
        {
            ref var entry = ref cluster.Entry2;
            move = entry.BestMove;
            score = entry.Score;
            staticEval = entry.Evaluation;
            depth = entry.Depth;
            bound = entry.NodeType;
            return true;
        }

        if (cluster.Entry3.HashVerification == verify)
        {
            ref var entry = ref cluster.Entry3;
            move = entry.BestMove;
            score = entry.Score;
            staticEval = entry.Evaluation;
            depth = entry.Depth;
            bound = entry.NodeType;
            return true;
        }

        // Position not found
        move = 0;
        score = 0;
        staticEval = 0;
        depth = 0;
        bound = TranspositionNodeType.None;
        return false;
    }
}
