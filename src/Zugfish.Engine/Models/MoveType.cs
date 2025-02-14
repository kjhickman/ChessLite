namespace Zugfish.Engine.Models;

public enum MoveType : byte
{
    Quiet = 0,
    Capture = 1,
    DoublePawnPush = 2,
    EnPassant = 3,
    Castling = 4,
    PromoteToKnight = 5,
    PromoteToBishop = 6,
    PromoteToRook = 7,
    PromoteToQueen = 8,
    // TODO: Should Capture-Promotion be separate MoveTypes?
}
