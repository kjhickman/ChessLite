using System.Runtime.InteropServices;

namespace Veloce.Search;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TranspositionEntryCluster
{
    public TranspositionEntry Entry1;
    public TranspositionEntry Entry2;
    public TranspositionEntry Entry3;

#pragma warning disable CS0169 // Field is never used
    private ushort _padding; // 2 bytes of padding to align to 64 bytes
#pragma warning restore CS0169 // Field is never used
}
