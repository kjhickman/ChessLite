using Zugfish.Engine.Models;

namespace Zugfish.Engine.Extensions;

/// <summary>
/// Extension methods for working with Move and CompactMove
/// </summary>
public static class MoveExtensions
{
    /// <summary>
    /// Converts a Move to a CompactMove for storage in the transposition table
    /// </summary>
    public static CompactMove ToCompactMove(this Move move)
    {
        if (move == Move.NullMove) return 0;
        
        var from = (int)move.From;
        var to = (int)move.To;
        
        // Basic encoding: 6 bits for from (0-63), 6 bits for to (0-63),
        // and 4 bits for promotion
        var compact = (ushort)(from | (to << 6));
        
        // Add promotion info in the high 4 bits
        if (move.PromotedPieceType != PromotedPieceType.None)
        {
            compact |= (ushort)((int)move.PromotedPieceType << 12);
        }
        
        return new CompactMove(compact);
    }
    
    /// <summary>
    /// Finds a move in the move list that matches the given CompactMove
    /// </summary>
    public static Move FindMatchingMove(this CompactMove compactMove, Span<Move> moveList, int moveCount)
    {
        // Early exit for null move
        ushort packed = compactMove;
        if (packed == 0) return Move.NullMove;
        
        // Extract from and to squares
        var from = (Square)(packed & 0x3F);
        var to = (Square)((packed >> 6) & 0x3F);
        var promotedType = (PromotedPieceType)((packed >> 12) & 0xF);
        
        // Find matching move in the list
        for (var i = 0; i < moveCount; i++)
        {
            if (moveList[i].From == from && moveList[i].To == to)
            {
                // For promotions, check the promotion type too
                if (promotedType != PromotedPieceType.None)
                {
                    if (moveList[i].PromotedPieceType == promotedType)
                        return moveList[i];
                }
                else
                {
                    return moveList[i];
                }
            }
        }
        
        // If no match is found, return NullMove
        return Move.NullMove;
    }
}
