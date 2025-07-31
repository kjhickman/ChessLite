using Veloce.Core.Models;

namespace Veloce.Search;

public class NullEngineLogger : IEngineLogger
{
    public void LogSearchInfo(SearchInfo info)
    {
    }

    public void LogBestMove(Move? bestMove, Move? ponderMove = null)
    {
    }
}
