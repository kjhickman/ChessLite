namespace Veloce.Search.SearchAlgorithms;

/// <summary>
/// Shared helper methods for search algorithms to reduce code duplication
/// </summary>
public static class SearchHelpers
{
    /// <summary>
    /// Detects forced mate scores (allowing some buffer for mate distance)
    /// </summary>
    public static bool IsMateScore(int score)
    {
        if (score == int.MinValue) return false;
        return Math.Abs(score) > 9000;
    }

    /// <summary>
    /// Determines if a new score is better than an old score from the perspective of the player to move
    /// </summary>
    public static bool IsScoreBetter(int newScore, int oldScore, bool isWhiteToMove)
    {
        return isWhiteToMove ? newScore > oldScore : newScore < oldScore;
    }
}
