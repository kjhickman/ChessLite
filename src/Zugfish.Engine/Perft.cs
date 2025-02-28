using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public static class Perft
{
    public static long CountNodes(Position position, int depth)
    {
        if (depth == 0) return 1;

        Span<Move> moves = stackalloc Move[218]; // Max possible moves
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);

        if (depth == 1) return moveCount;

        long nodes = 0;
        for (var i = 0; i < moveCount; i++)
        {
            position.MakeMove(moves[i]);
            nodes += CountNodes(position, depth - 1);
            position.UndoMove();
        }

        return nodes;
    }

    // Method for debugging specific positions
    public static Dictionary<string, long> DividePerft(Position position, int depth)
    {
        var result = new Dictionary<string, long>();
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);

        for (var i = 0; i < moveCount; i++)
        {
            var moveStr = moves[i].ToString();
            position.MakeMove(moves[i]);
            var nodes = CountNodes(position, depth - 1);
            position.UndoMove();
            result[moveStr] = nodes;
        }

        return result;
    }
}
