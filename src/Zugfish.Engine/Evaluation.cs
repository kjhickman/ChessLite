using Zugfish.Engine.Models;
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

    public static int Evaluate(this Position position)
    {
        return EvaluateMaterial(position) + EvaluatePosition(position);
    }

    private static int EvaluateMaterial(Position position)
    {
        var materialScore = PawnValue * PopCount(position.WhitePawns) - PawnValue * PopCount(position.BlackPawns);
        materialScore += KnightValue * PopCount(position.WhiteKnights) - KnightValue * PopCount(position.BlackKnights);
        materialScore += BishopValue * PopCount(position.WhiteBishops) - BishopValue * PopCount(position.BlackBishops);
        materialScore += RookValue * PopCount(position.WhiteRooks) - RookValue * PopCount(position.BlackRooks);
        materialScore += QueenValue * PopCount(position.WhiteQueens) - QueenValue * PopCount(position.BlackQueens);

        return materialScore;
    }

    private static int EvaluatePosition(Position position)
    {
        var positionalScore = 0;
        positionalScore += EvaluatePiecePositions(position.WhitePawns, PawnTable, true);
        positionalScore += EvaluatePiecePositions(position.WhiteKnights, KnightTable, true);
        positionalScore += EvaluatePiecePositions(position.WhiteBishops, BishopTable, true);
        positionalScore += EvaluatePiecePositions(position.WhiteRooks, RookTable, true);
        positionalScore += EvaluatePiecePositions(position.WhiteQueens, QueenTable, true);
        positionalScore += EvaluatePiecePositions(position.WhiteKing, KingTable, true);

        positionalScore -= EvaluatePiecePositions(position.BlackPawns, PawnTable, false);
        positionalScore -= EvaluatePiecePositions(position.BlackKnights, KnightTable, false);
        positionalScore -= EvaluatePiecePositions(position.BlackBishops, BishopTable, false);
        positionalScore -= EvaluatePiecePositions(position.BlackRooks, RookTable, false);
        positionalScore -= EvaluatePiecePositions(position.BlackQueens, QueenTable, false);
        positionalScore -= EvaluatePiecePositions(position.BlackKing, KingTable, false);

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
