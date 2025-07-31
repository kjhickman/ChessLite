using System.Runtime.CompilerServices;
using Veloce.Core.Models;

namespace Veloce.Core.Extensions;

public static class SquareExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitboard ToMask(this Square square) => Bitboard.Mask(square);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRank(this Square square) => (int)square / 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFile(this Square square) => (int)square % 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this Square square) => square is >= Square.a1 and <= Square.h8;

    public static Square FromRankFile(int rank, int file) => (Square)(rank * 8 + file);
}
