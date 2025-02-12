using System.Runtime.CompilerServices;

namespace Zugfish.Engine;

public static class Translation
{
    /// <summary>
    /// Converts a file and rank to a square index (0–63). Assumes 'a'-'h' and '1'-'8'.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SquareFromUci(char file, char rank)
    {
        if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            throw new ArgumentException("Invalid UCI square.", nameof(file));

        return (rank - '1') * 8 + (file - 'a');
    }

    /// <summary>
    /// Converts a two-character UCI square (e.g. "e4") to its square index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SquareFromUci(ReadOnlySpan<char> square)
    {
        if (square.Length != 2)
            throw new ArgumentException("Invalid square length", nameof(square));

        char file = square[0];
        char rank = square[1];

        if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            throw new ArgumentException("Invalid UCI square.", nameof(file));

        return (rank - '1') * 8 + (file - 'a');
    }
}
