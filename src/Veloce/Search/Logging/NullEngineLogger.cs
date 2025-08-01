using Veloce.Core.Models;
using Veloce.Search.Interfaces;

namespace Veloce.Search.Logging;

public class NullEngineLogger : IEngineLogger
{
    public void LogSearchInfo(SearchInfo info)
    {
    }

    public void LogBestMove(Move? bestMove, Move? ponderMove = null)
    {
    }
}
