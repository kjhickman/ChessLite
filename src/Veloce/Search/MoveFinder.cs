using Veloce.Engine;
using Veloce.Search.Interfaces;
using Veloce.State;

namespace Veloce.Search;

public class MoveFinder
{
    private readonly ISearchAlgorithm _searchAlgorithm;
    private readonly IEngineLogger? _engineLogger;
    private readonly EngineSettings _settings;

    public MoveFinder(EngineSettings? settings = null, IEngineLogger? engineLogger = null)
    {
        _settings = settings ?? EngineSettings.Default;
        _engineLogger = engineLogger;
        _searchAlgorithm = SearchAlgorithmFactory.CreateSearchAlgorithm(_settings);
        if (_engineLogger != null) _searchAlgorithm.OnSearchInfoAvailable = _engineLogger.LogSearchInfo;
    }

    // Uses the provided search algorithm (for testing purposes)
    public MoveFinder(ISearchAlgorithm searchAlgorithm, EngineSettings? settings = null, IEngineLogger? engineLogger = null)
    {
        _settings = settings ?? EngineSettings.Default;
        _engineLogger = engineLogger;
        _searchAlgorithm = searchAlgorithm ?? throw new ArgumentNullException(nameof(searchAlgorithm));
        if (_engineLogger != null) _searchAlgorithm.OnSearchInfoAvailable = _engineLogger.LogSearchInfo;
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
        return result;
    }

    public void StopSearch()
    {
        _searchAlgorithm.ShouldStop = true;
    }

    public long NodesSearched => _searchAlgorithm.NodesSearched;
}
