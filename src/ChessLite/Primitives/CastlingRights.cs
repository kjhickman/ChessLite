using System.Runtime.CompilerServices;

namespace ChessLite.Primitives;

[Flags]
public enum CastlingRights : byte
{
    None = 0,
    WhiteKingside = 1,
    WhiteQueenside = 2,
    BlackKingside = 4,
    BlackQueenside = 8,
}

#pragma warning disable MA0048
public static class CastlingRightsExtensions
{
    extension(CastlingRights rights)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(CastlingRights other) => (rights & other) != CastlingRights.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CastlingRights Remove(CastlingRights other) => rights & ~other;
    }
}
