namespace Zugfish.Engine;

public static class Search
{
    public static Move FindBestMove(MoveGenerator moveGenerator, Board board, int depth)
    {
        var isMaximizing = board.WhiteToMove;
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = moveGenerator.GenerateLegalMoves(board, movesBuffer);
        var bestMove = movesBuffer[0];
        var bestScore = board.WhiteToMove ? int.MinValue : int.MaxValue;
        var alpha = int.MinValue;
        var beta = int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            board.MakeMove(move);
            var score = Minimax(moveGenerator, board, depth - 1, alpha, beta, !isMaximizing);
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

    private static int Minimax(MoveGenerator moveGenerator, Board board, int depth, int alpha, int beta, bool isMaximizing)
    {
        if (depth == 0)
        {
            return board.Evaluate();
        }

        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = moveGenerator.GenerateLegalMoves(board, movesBuffer);
        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            board.MakeMove(move);
            var score = Minimax(moveGenerator, board, depth - 1, alpha, beta, !isMaximizing);
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

        return bestScore;
    }
}
