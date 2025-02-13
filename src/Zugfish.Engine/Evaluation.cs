using static System.Numerics.BitOperations;

namespace Zugfish.Engine;

public static class Evaluation
{
    private const int PawnValue = 100;
    private const int KnightValue = 320;
    private const int BishopValue = 330;
    private const int RookValue = 500;
    private const int QueenValue = 900;

    private static readonly int[] PawnTable =
    [
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    ];

    private static readonly int[] KnightTable =
    [
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    ];

    private static readonly int[] BishopTable =
    [
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    ];

    private static readonly int[] RookTable =
    [
         0,  0,  0,  0,  0,  0,  0,  0,
         5, 10, 10, 10, 10, 10, 10,  5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
         0,  0,  0,  5,  5,  0,  0,  0
    ];

    private static readonly int[] QueenTable =
    [
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    ];

    private static readonly int[] KingTable =
    [
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    ];

    public static int Evaluate(this Board board)
    {
        return EvaluateMaterial(board) + EvaluatePosition(board);
    }

    private static int EvaluateMaterial(Board board)
    {
        var materialScore = PawnValue * PopCount(board.WhitePawns) - PawnValue * PopCount(board.BlackPawns);
        materialScore += KnightValue * PopCount(board.WhiteKnights) - KnightValue * PopCount(board.BlackKnights);
        materialScore += BishopValue * PopCount(board.WhiteBishops) - BishopValue * PopCount(board.BlackBishops);
        materialScore += RookValue * PopCount(board.WhiteRooks) - RookValue * PopCount(board.BlackRooks);
        materialScore += QueenValue * PopCount(board.WhiteQueens) - QueenValue * PopCount(board.BlackQueens);

        return materialScore;
    }

    private static int EvaluatePosition(Board board)
    {
        var positionalScore = 0;
        positionalScore += EvaluatePiecePositions(board.WhitePawns, PawnTable, true);
        positionalScore += EvaluatePiecePositions(board.WhiteKnights, KnightTable, true);
        positionalScore += EvaluatePiecePositions(board.WhiteBishops, BishopTable, true);
        positionalScore += EvaluatePiecePositions(board.WhiteRooks, RookTable, true);
        positionalScore += EvaluatePiecePositions(board.WhiteQueens, QueenTable, true);
        positionalScore += EvaluatePiecePositions(board.WhiteKing, KingTable, true);

        positionalScore -= EvaluatePiecePositions(board.BlackPawns, PawnTable, false);
        positionalScore -= EvaluatePiecePositions(board.BlackKnights, KnightTable, false);
        positionalScore -= EvaluatePiecePositions(board.BlackBishops, BishopTable, false);
        positionalScore -= EvaluatePiecePositions(board.BlackRooks, RookTable, false);
        positionalScore -= EvaluatePiecePositions(board.BlackQueens, QueenTable, false);
        positionalScore -= EvaluatePiecePositions(board.BlackKing, KingTable, false);

        return positionalScore;
    }

    private static int EvaluatePiecePositions(Bitboard pieces, int[] table, bool isWhite)
    {
        var score = 0;
        var currentPieces = pieces;
        while (currentPieces != 0)
        {
            var square = TrailingZeroCount(currentPieces);
            var index = isWhite ? square : 63 - square;
            score += table[index];

            currentPieces &= currentPieces - 1;
        }

        return score;
    }
}
