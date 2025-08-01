using Veloce.Engine;
using Veloce.Search.SearchAlgorithms;

namespace Veloce.Search;

public static class SearchAlgorithmFactory
{
    public static ISearchAlgorithm CreateSearchAlgorithm(EngineSettings settings)
    {
        if (settings.Threads > 1)
        {
            return new SharedHashTableSearch(new ConsoleEngineLogger(), settings);
        }

        return new SingleThreadedAlphaBetaSearch(settings);
    }
}
