using System.Runtime.CompilerServices;
using Veloce.Core.Models;

namespace Veloce.Core.Extensions;

public static class CastlingRightsExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this CastlingRights rights, CastlingRights other) => (rights & other) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CastlingRights Remove(this CastlingRights rights, CastlingRights other) => rights & ~other;
}
