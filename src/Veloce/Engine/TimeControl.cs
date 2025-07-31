namespace Veloce.Engine;

public readonly struct TimeControl
{
    public TimeControl(int timeLeft, int increment = -1, int movesToGo = -1)
    {
        TimeLeft = timeLeft;
        Increment = increment;
        MovesToGo = movesToGo;
    }

    public int TimeLeft { get; }
    public int Increment { get; }
    public int MovesToGo { get; }

    public static TimeControl Infinite => new(-1);
}
