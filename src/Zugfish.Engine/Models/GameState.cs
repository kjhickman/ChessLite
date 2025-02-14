namespace Zugfish.Engine.Models;

public enum GameState
{
    Ongoing,
    Checkmate,
    Stalemate,
    DrawFiftyMove,
    DrawRepetition,
    DrawInsufficientMaterial,
}
