using System.Runtime.InteropServices;

namespace Veloce.Core;

[StructLayout(LayoutKind.Auto)]
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