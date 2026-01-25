using System.Runtime.CompilerServices;

namespace ChessLite.Primitives;

/// <summary>
/// Represents the castling rights available in a chess position.
/// </summary>
[Flags]
public enum CastlingRights : byte
{
    /// <summary>No castling rights available.</summary>
    None = 0,

    /// <summary>White can castle kingside (O-O).</summary>
    WhiteKingside = 1,

    /// <summary>White can castle queenside (O-O-O).</summary>
    WhiteQueenside = 2,

    /// <summary>Black can castle kingside (O-O).</summary>
    BlackKingside = 4,

    /// <summary>Black can castle queenside (O-O-O).</summary>
    BlackQueenside = 8,
}

/// <summary>
/// Provides extension methods for <see cref="CastlingRights"/>.
/// </summary>
public static class CastlingRightsExtensions
{
    extension(CastlingRights rights)
    {
        /// <summary>
        /// Determines whether the current castling rights include the specified rights.
        /// </summary>
        /// <param name="other">The castling rights to check for.</param>
        /// <returns><c>true</c> if the specified rights are present; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(CastlingRights other) => (rights & other) != CastlingRights.None;

        /// <summary>
        /// Returns a new <see cref="CastlingRights"/> value with the specified rights removed.
        /// </summary>
        /// <param name="other">The castling rights to remove.</param>
        /// <returns>The castling rights with the specified rights cleared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CastlingRights Remove(CastlingRights other) => rights & ~other;
    }
}
