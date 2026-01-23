namespace ChessLite;

public enum GameState
{
    Ongoing,
    Checkmate,
    Stalemate,
    DrawByFiftyMoveRule,
    DrawByRepetition,
    DrawByInsufficientMaterial
}

public static class GameStateExtensions
{
    public static bool IsGameOver(this GameState state) => state != GameState.Ongoing;
    
    public static bool IsDraw(this GameState state) => state is 
        GameState.Stalemate or 
        GameState.DrawByFiftyMoveRule or 
        GameState.DrawByRepetition or 
        GameState.DrawByInsufficientMaterial;
}
