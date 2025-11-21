using System.Diagnostics;
using Veloce.Core;
using Veloce.Engine;
using Veloce.Evaluation;
using Veloce.Movement;
using Veloce.Search.Interfaces;
using Veloce.Search.Transposition;
using Veloce.State;

namespace Veloce.Search.SearchAlgorithms;

/// <summary>
/// Single-threaded alpha-beta search algorithm implementation
/// </summary>
public class SingleThreadedAlphaBetaSearch : ISearchAlgorithm
{
    private readonly TranspositionTable _transpositionTable;
    private readonly EngineSettings _settings;

    // Search state
    private volatile bool _shouldStop;
    private long _nodesSearched;
    private readonly int[] _searchDepthNodes = new int[100];
    private TimeSpan _searchTimeLimit;
    private long _searchStartTimeStamp;
    private CancellationToken _cancellationToken;

    public SingleThreadedAlphaBetaSearch(EngineSettings? settings = null)
    {
        _settings = settings ?? EngineSettings.Default;
        _transpositionTable = new TranspositionTable(_settings.TranspositionTableSizeMb);
    }

    public long NodesSearched => _nodesSearched;

    public Action<SearchInfo>? OnSearchInfoAvailable { get; set; }

    public bool ShouldStop
    {
        get => _shouldStop;
        set => _shouldStop = value;
    }

    public SearchResult FindBestMove(Game game, int maxDepth, int timeLimit, CancellationToken cancellationToken = default)
    {
        _shouldStop = false;
        _cancellationToken = cancellationToken;
        ResetSearchCounters();
        _searchStartTimeStamp = Stopwatch.GetTimestamp();
        _searchTimeLimit = timeLimit > 0 ? TimeSpan.FromMilliseconds(timeLimit) : TimeSpan.MaxValue;

        // Start a new search generation
        _transpositionTable.NewSearch();

        var result = new SearchResult();
        for (var depth = 1; depth <= maxDepth; depth++)
        {
            if (ShouldStopSearch())
            {
                break;
            }

            var iterationResult = SearchAtDepth(game, depth);

            var elapsedTime = Stopwatch.GetElapsedTime(_searchStartTimeStamp);
            var searchInfo = new SearchInfo
            {
                Depth = depth,
                Score = game.Position.WhiteToMove ? iterationResult.Score : -iterationResult.Score,
                IsMateScore = SearchHelpers.IsMateScore(iterationResult.Score),
                NodesSearched = _nodesSearched,
                TimeElapsed = elapsedTime,
                NodesPerSecond = (long)(_nodesSearched / elapsedTime.TotalSeconds),
                HashFull = _transpositionTable.GetOccupancy(),
            };

            OnSearchInfoAvailable?.Invoke(searchInfo);

            if (!_shouldStop)
            {
                result = iterationResult;
                result.Depth = depth;
                result.NodesSearched = _nodesSearched;
                result.TimeElapsed = elapsedTime;
            }

            if (_shouldStop || SearchHelpers.IsMateScore(iterationResult.Score))
            {
                break;
            }
        }

        return result;
    }

    public void Reset()
    {
        _transpositionTable.Clear();
        _shouldStop = false;
        _nodesSearched = 0;
        Array.Clear(_searchDepthNodes, 0, _searchDepthNodes.Length);
    }

    private void ResetSearchCounters()
    {
        _nodesSearched = 0;
        Array.Clear(_searchDepthNodes, 0, _searchDepthNodes.Length);
    }

    private bool ShouldStopSearch()
    {
        return _shouldStop ||
               _cancellationToken.IsCancellationRequested ||
               Stopwatch.GetElapsedTime(_searchStartTimeStamp) > _searchTimeLimit;
    }

    private SearchResult SearchAtDepth(Game game, int depth)
    {
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(game.Position, movesBuffer);

        if (moveCount == 0)
        {
            // Checkmate or stalemate
            var gameState = game.IsInCheck() ? GameState.Checkmate : GameState.Stalemate;
            var score = gameState == GameState.Checkmate
                ? game.Position.WhiteToMove ? -10000 : 10000
                : 0;
            return new SearchResult
            {
                BestMove = null,
                Score = score,
                GameState = gameState,
            };
        }

        var position = game.Position;
        var ttMove = Move.NullMove;
        if (_transpositionTable.Probe(position.ZobristHash, out var ttCompactMove, out _, out _, out _, out _))
        {
            if (ttCompactMove != 0) // Check if not a null move
            {
                ttMove = ttCompactMove.FindMatchingMove(movesBuffer, moveCount);
            }
        }

        MoveOrdering.OrderMoves(movesBuffer, moveCount, ttMove);

        var isMaximizing = position.WhiteToMove;
        var bestMove = movesBuffer[0];
        var bestScore = position.WhiteToMove ? int.MinValue : int.MaxValue;
        var alpha = int.MinValue;
        var beta = int.MaxValue;
        var evaluatedAnyMove = false;

        for (var i = 0; i < moveCount; i++)
        {
            if (ShouldStopSearch())
            {
                _shouldStop = true;
                break;
            }

            var move = movesBuffer[i];
            game.MakeMove(move);

            var context = SearchContext.CreateRoot(depth - 1, alpha, beta);
            var score = AlphaBeta(game, context, !isMaximizing).Score;

            game.UndoMove();

            evaluatedAnyMove = true;

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

        // If we didn't evaluate any moves due to timeout, use static evaluation
        if (!evaluatedAnyMove)
        {
            bestScore = position.Evaluate();
        }

        if (!_shouldStop)
        {
            _transpositionTable.Store(
                position.ZobristHash,
                bestMove.ToCompactMove(),
                (short)bestScore,
                (short)position.Evaluate(),
                (byte)depth,
                TranspositionNodeType.Exact);
        }

        return new SearchResult
        {
            BestMove = bestMove,
            Score = bestScore,
            GameState = GameState.Ongoing,
        };
    }

    private EvaluationResult AlphaBeta(Game game, SearchContext context, bool isMaximizing)
    {
        _nodesSearched++;
        _searchDepthNodes[context.Depth]++;
        context.Stats = context.Stats with { NodesSearched = context.Stats.NodesSearched + 1 };

        if ((_nodesSearched & 4095) == 0 && ShouldStopSearch())
        {
            _shouldStop = true;
            return new EvaluationResult(0, GameState.Ongoing);
        }

        if (game.IsDrawByFiftyMoves())
        {
            return new EvaluationResult(0, GameState.DrawFiftyMove);
        }

        if (game.IsDrawByInsufficientMaterial())
        {
            return new EvaluationResult(0, GameState.DrawInsufficientMaterial);
        }

        if (game.IsDrawByRepetition())
        {
            return new EvaluationResult(0, GameState.DrawRepetition);
        }

        // Transposition table lookup
        var ttHit = false;
        var ttMove = Move.NullMove;
        var position = game.Position;
        if (_transpositionTable.Probe(position.ZobristHash, out var ttCompactMove, out var ttScore, out var ttEval, out var ttDepth, out var ttBound))
        {
            ttHit = true;

            // Only use TT entries if their depth is sufficient
            if (ttDepth >= context.Depth)
            {
                // We can use the TT score if:
                // 1. It's an exact score, or
                // 2. It's a beta bound and score >= beta, or
                // 3. It's an alpha bound and score <= alpha
                switch (ttBound)
                {
                    case TranspositionNodeType.Exact:
                        context.Stats = context.Stats with { TranspositionHits = context.Stats.TranspositionHits + 1 };
                        return new EvaluationResult(ttScore, GameState.Ongoing);
                    case TranspositionNodeType.Alpha when ttScore <= context.Alpha:
                        context.Stats = context.Stats with { TranspositionHits = context.Stats.TranspositionHits + 1 };
                        return new EvaluationResult(ttScore, GameState.Ongoing);
                    case TranspositionNodeType.Beta when ttScore >= context.Beta:
                        context.Stats = context.Stats with { TranspositionHits = context.Stats.TranspositionHits + 1 };
                        return new EvaluationResult(ttScore, GameState.Ongoing);
                }
            }
        }

        // Check for leaf nodes
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, movesBuffer);

        if (moveCount == 0)
        {
            return game.IsInCheck()
                ? new EvaluationResult(isMaximizing ? -10000 + context.Ply : 10000 - context.Ply, GameState.Checkmate)
                : new EvaluationResult(0, GameState.Stalemate);
        }

        if (context.Depth <= 0)
        {
            // Reached depth limit, evaluate the position
            return new EvaluationResult(position.Evaluate(), GameState.Ongoing);
        }

        // Move ordering - try the TT move first if we have one
        if (ttHit && ttCompactMove != 0)
        {
            ttMove = ttCompactMove.FindMatchingMove(movesBuffer, moveCount);
        }
        MoveOrdering.OrderMoves(movesBuffer, moveCount, ttMove);

        var originalAlpha = context.Alpha;
        var originalBeta = context.Beta;
        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;
        var bestMove = Move.NullMove;
        var evaluatedAnyMove = false;

        // Main search loop
        for (var i = 0; i < moveCount; i++)
        {
            if (_shouldStop) break;

            var move = movesBuffer[i];
            game.MakeMove(move);
            var childContext = context.CreateChild(context.Depth - 1, context.Alpha, context.Beta);
            var score = AlphaBeta(game, childContext, !isMaximizing).Score;
            game.UndoMove();

            evaluatedAnyMove = true;

            if (isMaximizing)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                    if (context.Ply < context.PrincipalVariation.Length)
                    {
                        context.PrincipalVariation[context.Ply] = move;
                    }
                }
                context.Alpha = Math.Max(context.Alpha, score);
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                    if (context.Ply < context.PrincipalVariation.Length)
                    {
                        context.PrincipalVariation[context.Ply] = move;
                    }
                }
                context.Beta = Math.Min(context.Beta, score);
            }

            if (context.Beta <= context.Alpha)
            {
                context.Stats = context.Stats with { AlphaBetaCutoffs = context.Stats.AlphaBetaCutoffs + 1 };
                break;
            }
        }

        // Don't store anything if we had to abort
        if (_shouldStop)
        {
            // If we didn't evaluate any moves due to timeout, use static evaluation
            if (!evaluatedAnyMove)
            {
                bestScore = position.Evaluate();
            }
            return new EvaluationResult(bestScore, GameState.Ongoing);
        }

        // Save this position to the transposition table
        var nodeType = TranspositionNodeType.Exact;
        if (bestScore <= originalAlpha)
        {
            nodeType = TranspositionNodeType.Alpha;
        }
        else if (bestScore >= originalBeta)
        {
            nodeType = TranspositionNodeType.Beta;
        }

        _transpositionTable.Store(
            position.ZobristHash,
            bestMove.ToCompactMove(),
            (short)bestScore,
            (short)position.Evaluate(), // Evaluate position for static evaluation
            (byte)context.Depth,
            nodeType);

        return new EvaluationResult(bestScore, GameState.Ongoing);
    }
}
