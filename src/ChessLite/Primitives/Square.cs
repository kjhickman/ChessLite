// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace ChessLite.Primitives;

/// <summary>
/// Represents a square on the chess board, indexed 0-63 from a1 to h8.
/// </summary>
/// <remarks>
/// Square indices are laid out as follows:
/// <code>
///   noWe        north         noEa
///          +7    +8    +9
///              \  |  /
///  west    -1 &lt;-  0 -&gt; +1    east
///              /  |  \
///          -9    -8    -7
///  soWe         south        soEa
/// </code>
/// </remarks>
public enum Square
{
    a1, b1, c1, d1, e1, f1, g1, h1,
    a2, b2, c2, d2, e2, f2, g2, h2,
    a3, b3, c3, d3, e3, f3, g3, h3,
    a4, b4, c4, d4, e4, f4, g4, h4,
    a5, b5, c5, d5, e5, f5, g5, h5,
    a6, b6, c6, d6, e6, f6, g6, h6,
    a7, b7, c7, d7, e7, f7, g7, h7,
    a8, b8, c8, d8, e8, f8, g8, h8,

    /// <summary>Represents no square or an invalid square.</summary>
    None = -1,
}

/// <summary>
/// Provides extension methods for <see cref="Square"/>.
/// </summary>
public static class SquareExtensions
{
    extension(Square square)
    {
        /// <summary>
        /// Converts the square to a bitboard with a single bit set at this square's position.
        /// </summary>
        /// <returns>A bitboard mask for this square.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard ToMask() => Bitboard.Mask(square);

        /// <summary>
        /// Gets the rank (row) index of this square, where 0 is the first rank.
        /// </summary>
        /// <returns>The rank index (0-7).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRank() => (int)square / 8;

        /// <summary>
        /// Gets the file (column) index of this square, where 0 is the a-file.
        /// </summary>
        /// <returns>The file index (0-7).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFile() => (int)square % 8;

        /// <summary>
        /// Determines whether this square is a valid board square (a1 through h8).
        /// </summary>
        /// <returns><c>true</c> if the square is valid; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => square is >= Square.a1 and <= Square.h8;
    }

    /// <summary>
    /// Creates a square from rank and file indices.
    /// </summary>
    /// <param name="rank">The rank index (0-7).</param>
    /// <param name="file">The file index (0-7).</param>
    /// <returns>The square at the specified rank and file.</returns>
    public static Square FromRankFile(int rank, int file) => (Square)(rank * 8 + file);
}
