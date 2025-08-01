using Veloce.Core.Models;
using Veloce.Search;
using Veloce.State;

namespace Veloce.Engine;

public class VeloceEngine
{
    private readonly MoveFinder _moveFinder;
    private readonly EngineSettings _engineSettings;
    private Game _game;

    public VeloceEngine()
    {
        _engineSettings = EngineSettings.Default;
        _game = new Game();
        IEngineLogger engineLogger = new ConsoleEngineLogger();
        _moveFinder = new MoveFinder(engineLogger, _engineSettings);
    }

    public VeloceEngine(IEngineLogger engineLogger, EngineSettings engineSettings)
    {
        _engineSettings = engineSettings;
        _game = new Game();
        _moveFinder = new MoveFinder(engineLogger, _engineSettings);
    }

    public Move? FindBestMove()
    {
        return _moveFinder.FindBestMove(_game).BestMove;
    }

    public SearchResult FindBestMove(TimeControl timeControl)
    {
        var timeToSearchMs = TimeManagement.CalculateMoveTime(timeControl);
        return _moveFinder.FindBestMove(_game, timeToSearchMs);
    }

    public SearchResult FindBestMove(int timeToSearchMs)
    {
        return _moveFinder.FindBestMove(_game, timeToSearchMs);
    }

    public void MakeMove(Move move)
    {
        _game.MakeMove(move);
    }

    public void NewGame()
    {
        _game = new Game();
        _moveFinder.Reset();
    }

    public void SetPosition(Position position)
    {
        _game.SetPosition(position);
    }
}
