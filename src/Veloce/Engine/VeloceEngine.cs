using Veloce.Core.Models;
using Veloce.Search;
using Veloce.Search.Interfaces;
using Veloce.Search.Logging;
using Veloce.State;

namespace Veloce.Engine;

public class VeloceEngine
{
    private MoveFinder _moveFinder;
    private readonly EngineSettings _engineSettings;
    private readonly IEngineLogger _engineLogger;
    private Game _game;

    public VeloceEngine()
    {
        _engineSettings = EngineSettings.Default;
        _game = new Game();
        _engineLogger = new ConsoleEngineLogger();
        _moveFinder = new MoveFinder(_engineLogger, _engineSettings);
    }

    public VeloceEngine(IEngineLogger engineLogger, EngineSettings engineSettings)
    {
        _engineSettings = engineSettings;
        _engineLogger = engineLogger;
        _game = new Game();
        _moveFinder = new MoveFinder(_engineLogger, _engineSettings);
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

    public void SetThreads(int threads)
    {
        _engineSettings.SetThreads(threads);
        _moveFinder = new MoveFinder(_engineLogger, _engineSettings);
    }

    public void SetHashSize(int hashSizeMb)
    {
        _engineSettings.TranspositionTableSizeMb = Math.Max(1, Math.Min(128, hashSizeMb));
    }
}
