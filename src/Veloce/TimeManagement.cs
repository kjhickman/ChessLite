namespace Veloce;

public static class TimeManagement
{
    public static int CalculateMoveTime(TimeControl timeControl)
    {
        const int minimumMoveTimeMs = 50;
        const int timeBufferMs = 200;
        const int defaultMovesToGo = 25;

        if (timeControl.TimeLeft <= 0)
        {
            return minimumMoveTimeMs;
        }

        var movesToGo = timeControl.MovesToGo > 0 ? timeControl.MovesToGo : defaultMovesToGo;
        var increment = Math.Max(0, timeControl.Increment);

        // Use slightly less time than evenly distributed
        // Apply a scaling factor to be more conservative early in the game
        const double scalingFactor = 0.9;
        var moveTime = (int)(scalingFactor * timeControl.TimeLeft / movesToGo);

        // Add some of the increment (not all, to account for lag)
        if (increment > 0)
        {
            moveTime += (int)(increment * 0.75);
        }

        // Apply safety buffer but ensure we have a minimum time
        moveTime = Math.Max(minimumMoveTimeMs, moveTime - timeBufferMs);

        // Ensure we don't use more than a percentage of remaining time
        var maxTimeToUse = (int)(timeControl.TimeLeft * 0.3);
        return Math.Min(moveTime, maxTimeToUse);
    }
}
