namespace Veloce;

public class EngineSettings
{
    // Meta engine settings
    public int Depth { get; set; }

    // UCI options
    public int TranspositionTableSizeMb { get; set; }
    public int Threads { get; set; }

    public static EngineSettings Default => new()
    {
        Depth = 8,
        TranspositionTableSizeMb = 16,
        Threads = 1
    };
}
