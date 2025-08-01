using Veloce.Engine;
using Veloce.Search.Interfaces;
using Veloce.Search.Logging;
using Veloce.State;

namespace Veloce.Search;

public class MoveFinder
{
    private readonly ISearchAlgorithm _searchAlgorithm;
    private readonly IEngineLogger _engineLogger;
    private readonly EngineSettings _settings;

    public MoveFinder(IEngineLogger? engineLogger = null, EngineSettings? settings = null)
    {
        _engineLogger = engineLogger ?? new NullEngineLogger();
        _settings = settings ?? EngineSettings.Default;
        _searchAlgorithm = SearchAlgorithmFactory.CreateSearchAlgorithm(_settings);
        _searchAlgorithm.OnSearchInfoAvailable = _engineLogger.LogSearchInfo;
    }

    // Uses the provided search algorithm (for testing purposes)
    public MoveFinder(ISearchAlgorithm searchAlgorithm, IEngineLogger? engineLogger = null, EngineSettings? settings = null)
    {
        _searchAlgorithm = searchAlgorithm ?? throw new ArgumentNullException(nameof(searchAlgorithm));
        _engineLogger = engineLogger ?? new NullEngineLogger();
        _settings = settings ?? EngineSettings.Default;
        _searchAlgorithm.OnSearchInfoAvailable = _engineLogger.LogSearchInfo;
    }

    public void Reset()
    {
        _searchAlgorithm.Reset();
    }

    public SearchResult FindBestMove(Game game, int timeLimit = -1)
    {
        using var cts = new CancellationTokenSource();
        if (timeLimit > 0)
        {
            cts.CancelAfter(timeLimit);
        }

        var result = _searchAlgorithm.FindBestMove(game, _settings.MaxDepth, timeLimit, cts.Token);
        
        _engineLogger.LogBestMove(result.BestMove);
        return result;
    }

    public void StopSearch()
    {
        _searchAlgorithm.ShouldStop = true;
    }

    public long NodesSearched => _searchAlgorithm.NodesSearched;
}
