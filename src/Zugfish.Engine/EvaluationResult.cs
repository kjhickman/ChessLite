namespace Zugfish.Engine;

public struct EvaluationResult
{
    public int Score { get; }
    public GameState State { get; init; }

    public EvaluationResult(int score, GameState state)
    {
        Score = score;
        State = state;
    }
}