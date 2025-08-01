using Veloce.Core;

namespace Veloce.Search;

public static class MoveOrdering
{
    /// <summary>
    /// Orders the moves in descending order according to a simple heuristic.
    /// Moves matching the TT best move are given a huge bonus; capture moves are scored using MVV-LVA;
    /// promotion moves are also boosted.
    /// </summary>
    public static void OrderMoves(Span<Move> moves, int moveCount, Move ttMove, int threadId = 0)
    {
        // Compute a score for each move
        Span<int> scores = stackalloc int[moveCount];
        for (var i = 0; i < moveCount; i++)
        {
            scores[i] = ScoreMove(moves[i], ttMove);

            // Add small random factor for helper threads to explore different move orders
            if (threadId > 0)
            {
                scores[i] += Random.Shared.Next(-10, 11);
            }
        }

        // A simple quadratic sort on the small move list
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
    private static int ScoreMove(Move move, Move ttMove)
    {
        // Transposition table best move gets the highest score
        if (move.Equals(ttMove))
        {
            return 1000000;
        }

        var score = 0;
        if (move.IsCapture)
        {
            // MVV-LVA: bonus = (captured value - mover value) plus a base bonus
            var captured = move.CapturedPieceType;
            var mover = move.PieceType;
            score = GetPieceValue(captured) - GetPieceValue(mover) + 10000;
        }
        else if (move.PromotedPieceType != PromotedPieceType.None)
        {
            // Give a bonus based on the promotion piece (promotion to queen is best)
            score = GetPromotionValue(move.PromotedPieceType) + 800; // extra bonus for promotion moves
        }

        return score;
    }

    /// <summary>
    /// Returns a basic material value for a piece type
    /// </summary>
    private static int GetPieceValue(PieceType piece)
    {
        return piece switch
        {
            PieceType.WhitePawn or PieceType.BlackPawn => 100,
            PieceType.WhiteKnight or PieceType.BlackKnight => 320,
            PieceType.WhiteBishop or PieceType.BlackBishop => 330,
            PieceType.WhiteRook or PieceType.BlackRook => 500,
            PieceType.WhiteQueen or PieceType.BlackQueen => 900,
            _ => 0,
        };
    }

    /// <summary>
    /// Returns the value of a promoted piece type
    /// </summary>
    private static int GetPromotionValue(PromotedPieceType piece)
    {
        return piece switch
        {
            PromotedPieceType.Queen => 900,
            PromotedPieceType.Rook => 500,
            PromotedPieceType.Bishop => 330,
            PromotedPieceType.Knight => 320,
            _ => 0,
        };
    }
}
