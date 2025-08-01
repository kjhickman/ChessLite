using System.Diagnostics;
using Veloce.Core.Extensions;
using Veloce.Core.Models;
using Veloce.Engine;
using Veloce.Evaluation;
using Veloce.Movement;
using Veloce.State;

namespace Veloce.Search.SearchAlgorithms;

public class SharedHashTableSearch : ISearchAlgorithm
{
    private readonly TranspositionTable _sharedTranspositionTable;
    private readonly EngineSettings _settings;

    // Thread coordination
    private volatile bool _shouldStop;
    private long _totalNodesSearched;
    private readonly int _threadCount;
    private SearchResult _bestResult;
    private readonly Lock _resultLock = new();

    // Search state
    private long _searchStartTime;
    private TimeSpan _timeLimit;

    public SharedHashTableSearch(EngineSettings? settings = null)
    {
        _settings = settings ?? EngineSettings.Default;
        _sharedTranspositionTable = new TranspositionTable(_settings.TranspositionTableSizeMb);
        _threadCount = Math.Max(1, Environment.ProcessorCount - 1); // Leave one core free
    }

    public long NodesSearched => Interlocked.Read(ref _totalNodesSearched);

    public Action<SearchInfo>? OnSearchInfoAvailable { get; set; }

    public bool ShouldStop
    {
        get => _shouldStop;
        set => _shouldStop = value;
    }

    public SearchResult FindBestMove(Game game, int maxDepth, int timeLimit, CancellationToken cancellationToken = default)
    {
        // Initialize search state
        _shouldStop = false;
        Interlocked.Exchange(ref _totalNodesSearched, 0);
        _searchStartTime = Stopwatch.GetTimestamp();
        _timeLimit = timeLimit > 0 ? TimeSpan.FromMilliseconds(timeLimit) : TimeSpan.MaxValue;
        _bestResult = new SearchResult { Score = game.Position.WhiteToMove ? int.MinValue : int.MaxValue };

        // Start new search generation in shared table
        _sharedTranspositionTable.NewSearch();

        // Create and start worker threads
        var tasks = new Task[_threadCount];
        for (int i = 0; i < _threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() => ThreadWorker(game, maxDepth, threadId, cancellationToken), cancellationToken);
        }

        // Wait for all threads to complete or time/cancellation
        try
        {
            Task.WaitAll(tasks, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _shouldStop = true;
            // Give threads a moment to notice the stop flag
            Task.WaitAll(tasks, TimeSpan.FromMilliseconds(100));
        }

        // Finalize the result
        lock (_resultLock)
        {
            _bestResult.NodesSearched = NodesSearched;
            _bestResult.TimeElapsed = Stopwatch.GetElapsedTime(_searchStartTime);
            return _bestResult;
        }
    }

    public void Reset()
    {
        _shouldStop = true;
        Interlocked.Exchange(ref _totalNodesSearched, 0);
        _sharedTranspositionTable.Clear();
    }

    /// <summary>
    /// Worker thread that performs iterative deepening search with Lazy SMP variations
    /// </summary>
    private void ThreadWorker(Game game, int maxDepth, int threadId, CancellationToken cancellationToken)
    {
        // Create a copy of the game for this thread
        var threadGame = new Game();
        threadGame.SetPosition(game.Position.Clone());

        var threadMoveCount = 0;

        try
        {
            // Main thread (threadId 0) does normal search, others use variations
            for (int depth = 1; depth <= maxDepth && !_shouldStop && !cancellationToken.IsCancellationRequested; depth++)
            {
                // Check time limit periodically
                if (Stopwatch.GetElapsedTime(_searchStartTime) > _timeLimit)
                {
                    _shouldStop = true;
                    break;
                }

                // Apply Lazy SMP variations for helper threads
                var searchDepth = ApplyLazySmpVariations(depth, threadId, threadMoveCount);

                var result = SearchAtDepth(threadGame, searchDepth, threadId);

                // Update best result if this is better
                UpdateBestResult(result, depth);

                // Send search info if this is the main thread
                if (threadId == 0)
                {
                    SendSearchInfo(result, depth);
                }

                // Stop if we found a mate
                if (SearchHelpers.IsMateScore(result.Score))
                {
                    break;
                }

                threadMoveCount++;
            }
        }
        catch (OperationCanceledException)
        {
            // Thread was cancelled, exit gracefully
        }
    }

    /// <summary>
    /// Applies Lazy SMP variations to search depth and parameters
    /// </summary>
    private int ApplyLazySmpVariations(int depth, int threadId, int moveCount)
    {
        if (threadId == 0) return depth; // Main thread uses normal depth

        // Helper threads use various modifications:
        var variation = 0;

        // Depth variations - some threads search slightly different depths
        if (threadId % 4 == 1 && depth > 3)
            variation = -1; // Search one ply shallower
        else if (threadId % 4 == 2 && moveCount > 5)
            variation = 1;  // Search one ply deeper after some moves

        // Skip very shallow searches for some threads
        if (threadId > 2 && depth < 4)
            return Math.Max(1, depth + threadId - 2);

        return Math.Max(1, depth + variation);
    }

    /// <summary>
    /// Performs search at the given depth for this thread
    /// </summary>
    private SearchResult SearchAtDepth(Game game, int depth, int threadId)
    {
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(game.Position, movesBuffer);

        if (moveCount == 0)
        {
            // No legal moves - checkmate or stalemate
            var gameState = game.IsInCheck() ? GameState.Checkmate : GameState.Stalemate;
            var score = gameState == GameState.Checkmate
                ? (game.Position.WhiteToMove ? -10000 : 10000)
                : 0;
            return new SearchResult
            {
                BestMove = null,
                Score = score,
                GameState = gameState,
                Depth = depth
            };
        }

        // Get transposition table move hint
        var ttMove = Move.NullMove;
        if (_sharedTranspositionTable.Probe(game.Position.ZobristHash, out var ttCompactMove, out _, out _, out _, out _))
        {
            if (ttCompactMove != 0)
            {
                ttMove = ttCompactMove.FindMatchingMove(movesBuffer, moveCount);
            }
        }

        // Order moves
        MoveOrdering.OrderMoves(movesBuffer, moveCount, ttMove, threadId);

        var isMaximizing = game.Position.WhiteToMove;
        var bestMove = movesBuffer[0];
        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;
        var alpha = int.MinValue;
        var beta = int.MaxValue;

        // Search all moves
        for (int i = 0; i < moveCount && !_shouldStop; i++)
        {
            var move = movesBuffer[i];
            game.MakeMove(move);

            var score = AlphaBeta(game, depth - 1, alpha, beta, !isMaximizing, 1, threadId);

            game.UndoMove();

            if (isMaximizing)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, score);
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                beta = Math.Min(beta, score);
            }
        }

        // Store in transposition table
        if (!_shouldStop)
        {
            _sharedTranspositionTable.Store(
                game.Position.ZobristHash,
                bestMove.ToCompactMove(),
                (short)bestScore,
                (short)game.Position.Evaluate(),
                (byte)depth,
                TranspositionNodeType.Exact);
        }

        return new SearchResult
        {
            BestMove = bestMove,
            Score = bestScore,
            GameState = GameState.Ongoing,
            Depth = depth
        };
    }

    /// <summary>
    /// Alpha-beta search with shared transposition table
    /// </summary>
    private int AlphaBeta(Game game, int depth, int alpha, int beta, bool isMaximizing, int ply, int threadId)
    {
        // Increment node counter atomically
        Interlocked.Increment(ref _totalNodesSearched);

        // Check for time limit periodically
        if ((NodesSearched & 4095) == 0)
        {
            if (Stopwatch.GetElapsedTime(_searchStartTime) > _timeLimit)
            {
                _shouldStop = true;
                return 0;
            }
        }

        if (_shouldStop) return 0;

        // Check for draws
        if (game.IsDrawByFiftyMoves() || game.IsDrawByInsufficientMaterial() || game.IsDrawByRepetition())
        {
            return 0;
        }

        // Transposition table lookup
        var position = game.Position;
        if (_sharedTranspositionTable.Probe(position.ZobristHash, out var ttMove, out var ttScore, out _, out var ttDepth, out var ttBound))
        {
            if (ttDepth >= depth)
            {
                switch (ttBound)
                {
                    case TranspositionNodeType.Exact:
                        return ttScore;
                    case TranspositionNodeType.Beta when ttScore <= alpha:
                        return alpha;
                    case TranspositionNodeType.Alpha when ttScore >= beta:
                        return beta;
                }
            }
        }

        // Generate moves
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, movesBuffer);

        if (moveCount == 0)
        {
            // Terminal position
            return game.IsInCheck()
                ? (isMaximizing ? -10000 + ply : 10000 - ply)
                : 0;
        }

        if (depth <= 0)
        {
            // Leaf node - return static evaluation
            return position.Evaluate();
        }

        // Order moves
        var bestMove = Move.NullMove;
        if (ttMove != 0)
        {
            bestMove = ttMove.FindMatchingMove(movesBuffer, moveCount);
        }
        MoveOrdering.OrderMoves(movesBuffer, moveCount, bestMove, threadId);

        var originalAlpha = alpha;
        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;
        bestMove = Move.NullMove;

        // Search moves
        for (int i = 0; i < moveCount && !_shouldStop; i++)
        {
            var move = movesBuffer[i];
            game.MakeMove(move);

            var score = AlphaBeta(game, depth - 1, alpha, beta, !isMaximizing, ply + 1, threadId);

            game.UndoMove();

            if (isMaximizing)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, score);
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                beta = Math.Min(beta, score);
            }

            if (beta <= alpha) break; // Alpha-beta cutoff
        }

        // Store in transposition table
        if (!_shouldStop)
        {
            var nodeType = bestScore <= originalAlpha ? TranspositionNodeType.Beta :
                          bestScore >= beta ? TranspositionNodeType.Alpha :
                          TranspositionNodeType.Exact;

            _sharedTranspositionTable.Store(
                position.ZobristHash,
                bestMove.ToCompactMove(),
                (short)bestScore,
                (short)position.Evaluate(),
                (byte)depth,
                nodeType);
        }

        return bestScore;
    }


    /// <summary>
    /// Updates the best result if the new one is better
    /// </summary>
    private void UpdateBestResult(SearchResult result, int _)
    {
        lock (_resultLock)
        {
            var isBetter = false;

            // Prefer higher depth
            if (result.Depth > _bestResult.Depth)
            {
                isBetter = true;
            }
            else if (result.Depth == _bestResult.Depth)
            {
                // Same depth - compare scores
                if (_bestResult.BestMove == null ||
                    (result.BestMove != null && SearchHelpers.IsScoreBetter(result.Score, _bestResult.Score, result.BestMove != null)))
                {
                    isBetter = true;
                }
            }

            if (isBetter)
            {
                _bestResult = result;
            }
        }
    }

    /// <summary>
    /// Sends search information to the logger
    /// </summary>
    private void SendSearchInfo(SearchResult result, int depth)
    {
        var elapsed = Stopwatch.GetElapsedTime(_searchStartTime);
        var nodes = NodesSearched;

        var searchInfo = new SearchInfo
        {
            Depth = depth,
            Score = result.Score,
            IsMateScore = SearchHelpers.IsMateScore(result.Score),
            NodesSearched = nodes,
            TimeElapsed = elapsed,
            NodesPerSecond = elapsed.TotalSeconds > 0 ? (long)(nodes / elapsed.TotalSeconds) : 0,
            HashFull = _sharedTranspositionTable.GetOccupancy()
        };

        OnSearchInfoAvailable?.Invoke(searchInfo);
    }
}
