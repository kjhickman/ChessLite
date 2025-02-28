using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public class Search
{
    private readonly TranspositionTable _transpositionTable = new(1 << 20); // 1,048,576

    public Move? FindBestMove(Position position, int depth)
    {
        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, movesBuffer);

        if (moveCount == 0)
        {
            // checkmate or stalemate
            return null;
        }

        var ttMove = Move.NullMove;
        if (_transpositionTable.TryGet(position.ZobristHash, out var ttEntry) && !ttEntry.BestMove.Equals(default))
        {
            ttMove = ttEntry.BestMove;
        }
        OrderMoves(position, movesBuffer, moveCount, ttMove);

        var isMaximizing = position.WhiteToMove;
        var bestMove = movesBuffer[0];
        var bestScore = position.WhiteToMove ? int.MinValue : int.MaxValue;
        var alpha = int.MinValue;
        var beta = int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            position.MakeMove(move);
            var score = Minimax(position, depth - 1, alpha, beta, !isMaximizing).Score;
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

    private EvaluationResult Minimax(Position position, int depth, int alpha, int beta, bool isMaximizing)
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

        if (position.IsDrawByRepetition())
        {
            return new EvaluationResult(0, GameState.DrawRepetition);
        }

        if (_transpositionTable.TryGet(position.ZobristHash, out var ttEntry) && ttEntry.Depth >= depth)
        {
            switch (ttEntry.Flag)
            {
                case NodeType.Exact:
                    return new EvaluationResult(ttEntry.Score, GameState.Ongoing);
                case NodeType.Alpha:
                    alpha = Math.Max(alpha, ttEntry.Score);
                    break;
                case NodeType.Beta:
                    beta = Math.Min(beta, ttEntry.Score);
                    break;
            }
            if (alpha >= beta)
                return new EvaluationResult(ttEntry.Score, GameState.Ongoing);
        }

        Span<Move> movesBuffer = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, movesBuffer);

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

        var ttMove = Move.NullMove;
        if (_transpositionTable.TryGet(position.ZobristHash, out var entry) && !entry.BestMove.Equals(Move.NullMove))
        {
            ttMove = entry.BestMove;
        }
        OrderMoves(position, movesBuffer, moveCount, ttMove);

        var originalAlpha = alpha;
        var bestScore = isMaximizing ? int.MinValue : int.MaxValue;

        for (var i = 0; i < moveCount; i++)
        {
            var move = movesBuffer[i];
            position.MakeMove(move);
            var score = Minimax(position, depth - 1, alpha, beta, !isMaximizing).Score;
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

        NodeType flag;
        if (bestScore <= originalAlpha)
            flag = NodeType.Beta;
        else if (bestScore >= beta)
            flag = NodeType.Alpha;
        else
            flag = NodeType.Exact;

        _transpositionTable.Store(position.ZobristHash, depth, bestScore, flag, new Move());

        return new EvaluationResult(bestScore, GameState.Ongoing);
    }

    /// <summary>
    /// Orders the moves in descending order according to a simple heuristic.
    /// Moves matching the TT best move are given a huge bonus; capture moves are scored using MVV–LVA;
    /// promotion moves are also boosted.
    /// </summary>
    private void OrderMoves(Position pos, Span<Move> moves, int moveCount, Move ttMove)
    {
        // Compute a score for each move.
        Span<int> scores = stackalloc int[moveCount];
        for (var i = 0; i < moveCount; i++)
        {
            scores[i] = ScoreMove(pos, moves[i], ttMove);
        }

        // A simple (quadratic) sort on the small move list.
        for (var i = 0; i < moveCount - 1; i++)
        {
            for (var j = i + 1; j < moveCount; j++)
            {
                if (scores[j] <= scores[i]) continue;

                (moves[i], moves[j]) = (moves[j], moves[i]);
                (scores[i], scores[j]) = (scores[j], scores[i]);
            }
        }
    }

    /// <summary>
    /// Returns a score for the move. A higher score means the move is expected to be better.
    /// </summary>
    private int ScoreMove(Position pos, Move move, Move ttMove)
    {
        // Transposition table best move gets the highest score.
        if (move.Equals(ttMove))
        {
            return 1000000;
        }

        int score = 0;
        if (move.IsCapture)
        {
            // MVV-LVA: bonus = (captured value - mover value) plus a base bonus.
            var captured = GetCapturedPieceType(pos, move);
            var mover = GetPieceTypeAtSquare(pos, move.From);
            score = GetPieceValue(captured) - GetPieceValue(mover) + 10000;
        }
        else if (move.PromotedPieceType != PromotedPieceType.None)
        {
            // Give a bonus based on the promotion piece (promotion to queen is best)
            switch (move.PromotedPieceType)
            {
                case PromotedPieceType.Queen:
                    score = 900;
                    break;
                case PromotedPieceType.Rook:
                    score = 500;
                    break;
                case PromotedPieceType.Bishop:
                    score = 330;
                    break;
                case PromotedPieceType.Knight:
                    score = 320;
                    break;
                default:
                    score = 0;
                    break;
            }
            score += 800; // extra bonus for promotion moves
        }
        // Quiet moves currently get a baseline score (you could incorporate history heuristics here).
        return score;
    }

    /// <summary>
    /// Returns the piece type at the given square in the position.
    /// </summary>
    private PieceType GetPieceTypeAtSquare(Position pos, Square square)
    {
        var mask = Bitboard.Mask(square);
        if ((pos.WhitePawns & mask) != 0) return PieceType.WhitePawn;
        if ((pos.WhiteKnights & mask) != 0) return PieceType.WhiteKnight;
        if ((pos.WhiteBishops & mask) != 0) return PieceType.WhiteBishop;
        if ((pos.WhiteRooks & mask) != 0) return PieceType.WhiteRook;
        if ((pos.WhiteQueens & mask) != 0) return PieceType.WhiteQueen;
        if ((pos.WhiteKing & mask) != 0) return PieceType.WhiteKing;
        if ((pos.BlackPawns & mask) != 0) return PieceType.BlackPawn;
        if ((pos.BlackKnights & mask) != 0) return PieceType.BlackKnight;
        if ((pos.BlackBishops & mask) != 0) return PieceType.BlackBishop;
        if ((pos.BlackRooks & mask) != 0) return PieceType.BlackRook;
        if ((pos.BlackQueens & mask) != 0) return PieceType.BlackQueen;
        if ((pos.BlackKing & mask) != 0) return PieceType.BlackKing;
        return PieceType.None;
    }

    /// <summary>
    /// For capture moves, determines the piece type of the captured piece.
    /// For en passant, it computes the actual square of the captured pawn.
    /// </summary>
    private PieceType GetCapturedPieceType(Position pos, Move move)
    {
        Square capturedSquare;
        if (move.SpecialMoveType == SpecialMoveType.EnPassant)
        {
            // Use the same logic as in MakeMove: determine the pawn captured via en passant.
            // capturedSquare = move.To + (move.To > move.From ? -8 : 8);
            capturedSquare = pos.WhiteToMove ? move.To - 8 : move.To + 8;
        }
        else
        {
            capturedSquare = move.To;
        }
        return GetPieceTypeAtSquare(pos, capturedSquare);
    }

    /// <summary>
    /// Returns a basic material value for a piece type.
    /// </summary>
    private int GetPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.WhitePawn:
            case PieceType.BlackPawn:
                return 100;
            case PieceType.WhiteKnight:
            case PieceType.BlackKnight:
                return 320;
            case PieceType.WhiteBishop:
            case PieceType.BlackBishop:
                return 330;
            case PieceType.WhiteRook:
            case PieceType.BlackRook:
                return 500;
            case PieceType.WhiteQueen:
            case PieceType.BlackQueen:
                return 900;
            default:
                return 0;
        }
    }
}
