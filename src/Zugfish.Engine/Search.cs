namespace Zugfish.Engine;

public static class Search
{
    public static Move? FindBestMove(MoveGenerator moveGenerator, Board board, int depth)
    {
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = moveGenerator.GenerateLegalMoves(board, movesBuffer);

        if (moveCount == 0)
        {
            // checkmate or stalemate
            return null;
        }

        var isMaximizing = board.WhiteToMove;
        var bestMove = movesBuffer[0];
        var bestScore = board.WhiteToMove ? int.MinValue : int.MaxValue;
        var alpha = int.MinValue;
        var beta = int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            board.MakeMove(move);
            var score = Minimax(moveGenerator, board, depth - 1, alpha, beta, !isMaximizing).Score;
            board.UndoMove();

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

    private static EvaluationResult Minimax(MoveGenerator moveGenerator, Board board, int depth, int alpha, int beta, bool isMaximizing)
    {
        // if halfmove clock is 100 or more, the game is a draw
        if (board.HalfmoveClock >= 100)
        {
            return new EvaluationResult(0, GameState.DrawFiftyMove);
        }

        // if the position is a draw by insufficient material, the game is a draw
        // if (board.IsDrawByInsufficientMaterial())
        // {
        //     return new EvaluationResult(0, GameState.DrawInsufficientMaterial);
        // }

        // todo: if the position is a draw by repetition, the game is a draw

        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = moveGenerator.GenerateLegalMoves(board, movesBuffer);

        if (moveCount == 0)
        {
            return board.IsInCheck()
                ? new EvaluationResult(isMaximizing ? int.MinValue : int.MaxValue, GameState.Checkmate)
                : new EvaluationResult(0, GameState.Stalemate);
        }

        if (depth == 0)
        {
            return new EvaluationResult(board.Evaluate(), GameState.Ongoing);
        }

        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            board.MakeMove(move);
            var score = Minimax(moveGenerator, board, depth - 1, alpha, beta, !isMaximizing).Score;
            board.UndoMove();

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
