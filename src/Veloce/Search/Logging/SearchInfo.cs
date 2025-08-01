using System.Runtime.InteropServices;

namespace Veloce.Search.Logging;

/// <summary>
/// Container for search information that can be passed to loggers.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SearchInfo
{
    public int Depth { get; set; }
    public int Score { get; set; }
    public bool IsMateScore { get; set; }
    public long NodesSearched { get; set; }
    public TimeSpan TimeElapsed { get; set; }
    public long NodesPerSecond { get; set; }
    public int HashFull { get; set; }
}
