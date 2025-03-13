namespace Veloce;

public class EngineSettings
{
    // Meta engine settings
    public int MaxDepth { get; set; } = 16;

    // UCI options
    public int TranspositionTableSizeMb { get; set; } = 32;
    public int Threads { get; set; } = 1;

    public static EngineSettings Default => new()
    {
        MaxDepth = 16,
        TranspositionTableSizeMb = 32,
        Threads = 1
    };
}
