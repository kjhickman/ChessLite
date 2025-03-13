using Veloce.Models;

namespace Veloce;

public class Engine
{
    private readonly Search _search;
    private readonly EngineSettings _engineSettings;
    private Game _game;

    public Engine()
    {
        _engineSettings = EngineSettings.Default;
        _game = new Game();
        IEngineLogger engineLogger = new ConsoleEngineLogger();
        _search = new Search(engineLogger, _engineSettings);
    }

    public Engine(IEngineLogger engineLogger, EngineSettings engineSettings)
    {
        _engineSettings = engineSettings;
        _game = new Game();
        _search = new Search(engineLogger, _engineSettings);
    }

    public Move? FindBestMove()
    {
        return _search.FindBestMove(_game).BestMove;
    }

    public SearchResult FindBestMove(TimeControl timeControl)
    {
        var timeToSearchMs = TimeManagement.CalculateMoveTime(timeControl);
        return _search.FindBestMove(_game, timeToSearchMs);
    }

    public SearchResult FindBestMove(int timeToSearchMs)
    {
        return _search.FindBestMove(_game, timeToSearchMs);
    }

    public void MakeMove(Move move)
    {
        _game.MakeMove(move);
    }

    public void NewGame()
    {
        _game = new Game();
        _search.Reset();
    }

    public void SetPosition(Position position)
    {
        _game.SetPosition(position);
    }
}
