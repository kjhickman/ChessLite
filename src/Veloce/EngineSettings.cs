namespace Veloce;

public class EngineSettings
{
    // Meta engine settings
    public int Depth { get; set; }

    // UCI options
    public int HashSizeInMb { get; set; }
    public int Threads { get; set; }

    public static EngineSettings Default => new()
    {
        Depth = 8,
        HashSizeInMb = 16,
        Threads = 1
    };
}
