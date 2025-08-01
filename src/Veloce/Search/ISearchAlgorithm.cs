using Veloce.State;

namespace Veloce.Search;

public interface ISearchAlgorithm
{
    SearchResult FindBestMove(Game game, int maxDepth, int timeLimit, CancellationToken cancellationToken = default);
    void Reset();
    long NodesSearched { get; }
    Action<SearchInfo>? OnSearchInfoAvailable { get; set; }
    bool ShouldStop { get; set; }
}
