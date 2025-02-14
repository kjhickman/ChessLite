using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public static class Search
{
    public static Move? FindBestMove(MoveGenerator moveGenerator, Position position, int depth)
    {
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = moveGenerator.GenerateLegalMoves(position, movesBuffer);

        if (moveCount == 0)
        {
            // checkmate or stalemate
            return null;
        }

        var isMaximizing = position.WhiteToMove;
        var bestMove = movesBuffer[0];
        var bestScore = position.WhiteToMove ? int.MinValue : int.MaxValue;
        var alpha = int.MinValue;
        var beta = int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            position.MakeMove(move);
            var score = Minimax(moveGenerator, position, depth - 1, alpha, beta, !isMaximizing).Score;
            position.UndoMove();

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

            if (beta <= alpha)
            {
                break;
            }
        }

        return bestMove;
    }

    private static EvaluationResult Minimax(MoveGenerator moveGenerator, Position position, int depth, int alpha, int beta, bool isMaximizing)
    {
        // if halfmove clock is 100 or more, the game is a draw
        if (position.HalfmoveClock >= 100)
        {
            return new EvaluationResult(0, GameState.DrawFiftyMove);
        }

        // if the position is a draw by insufficient material, the game is a draw
        if (position.IsDrawByInsufficientMaterial())
        {
            return new EvaluationResult(0, GameState.DrawInsufficientMaterial);
        }

        // todo: if the position is a draw by repetition, the game is a draw
        if (position.IsDrawByRepetition())
        {
            return new EvaluationResult(0, GameState.DrawRepetition);
        }

        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = moveGenerator.GenerateLegalMoves(position, movesBuffer);

        if (moveCount == 0)
        {
            return position.IsInCheck()
                ? new EvaluationResult(isMaximizing ? int.MinValue : int.MaxValue, GameState.Checkmate)
                : new EvaluationResult(0, GameState.Stalemate);
        }

        if (depth == 0)
        {
            return new EvaluationResult(position.Evaluate(), GameState.Ongoing);
        }

        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            position.MakeMove(move);
            var score = Minimax(moveGenerator, position, depth - 1, alpha, beta, !isMaximizing).Score;
            position.UndoMove();

            if (isMaximizing)
            {
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, score);
            }
            else
            {
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, score);
            }

            if (beta <= alpha)
            {
                break;
            }
        }

        return new EvaluationResult(bestScore, GameState.Ongoing);
    }
}
