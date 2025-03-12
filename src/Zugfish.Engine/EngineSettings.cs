namespace Zugfish.Engine;

public class EngineSettings
{
    // Meta engine settings
    public int Depth { get; set; }

    // UCI options
    public int HashSizeInMb { get; set; }
    public int Threads { get; set; }

    public static EngineSettings Default => new()
    {
        Depth = 6,
        HashSizeInMb = 16,
        Threads = 1
    };
}