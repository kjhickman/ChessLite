using System.Runtime.CompilerServices;

namespace Zugfish.Engine.Models;

[Flags]
public enum CastlingRights : byte
{
    None = 0,
    WhiteKingside = 1,
    WhiteQueenside = 2,
    BlackKingside = 4,
    BlackQueenside = 8
}

public static class CastlingRightsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this CastlingRights rights, CastlingRights other) => (rights & other) != 0;
}
