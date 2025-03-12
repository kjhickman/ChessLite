using Zugfish.Engine.Models;

namespace Zugfish.Engine;

public class Engine
{
    private Position _position;
    private readonly Search _search;
    private readonly MoveExecutor _moveExecutor;
    private readonly EngineSettings _engineSettings;

    public Engine()
    {
        _engineSettings = EngineSettings.Default;
        _position = new Position();
        _moveExecutor = new MoveExecutor();
        IEngineLogger engineLogger = new ConsoleEngineLogger();
        _search = new Search(engineLogger, _moveExecutor, _engineSettings.HashSizeInMb);
    }

    public Engine(IEngineLogger engineLogger, EngineSettings engineSettings)
    {
        _engineSettings = engineSettings;
        _position = new Position();
        _moveExecutor = new MoveExecutor();
        _search = new Search(engineLogger, _moveExecutor, _engineSettings.HashSizeInMb);
    }

    public Move? FindBestMove()
    {
        return _search.FindBestMove(_position, _engineSettings.Depth).BestMove;
    }

    public void MakeMove(Move move)
    {
        _moveExecutor.MakeMove(_position, move);
    }

    public void NewGame()
    {
        _position = new Position();
        _moveExecutor.ClearMoveHistory();
        _search.Reset();
    }

    public void SetPosition(Position position)
    {
        _position = position;
    }
}
