using Veloce.Core;
using Veloce.Movement;

namespace Veloce.State;

public class Game
{
    private readonly MoveExecutor _moveExecutor;
    private readonly ulong[] _repetitionTable;
    private int _currentPly;

    public Position Position { get; set; }

    public Game()
    {
        Position = new Position();
        _moveExecutor = new MoveExecutor();
        _repetitionTable = new ulong[128];
        _currentPly = 0;
    }

    public Game(string fen)
    {
        Position = new Position(fen);
        _moveExecutor = new MoveExecutor();
        _repetitionTable = new ulong[128];
        _currentPly = 0;
    }

    public void MakeMove(Move move)
    {
        _moveExecutor.MakeMove(Position, move);
        _repetitionTable[_currentPly++] = Position.ZobristHash;
    }

    public void UndoMove()
    {
        _moveExecutor.UndoMove(Position);
        _currentPly--;
    }

    public void SetPosition(Position position)
    {
        Position = position;
        _currentPly = 0;
        Array.Clear(_repetitionTable, 0, _repetitionTable.Length);
    }

    public bool IsInCheck()
    {
        var kingSquare = Position.WhiteToMove ? Position.WhiteKing.GetFirstSquare() : Position.BlackKing.GetFirstSquare();
        return MoveGeneration.IsSquareAttacked(Position, kingSquare, byWhite: !Position.WhiteToMove);
    }

    public bool IsDrawByRepetition()
    {
        var count = 0;
        for (var i = 0; i < _currentPly; i++)
        {
            if (_repetitionTable[i] != Position.ZobristHash) continue;

            count++;
            if (count >= 3)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDrawByFiftyMoves()
    {
        return Position.HalfmoveClock >= 100;
    }

    public bool IsDrawByInsufficientMaterial()
    {
        var whiteMaterial = Position.WhitePieces & ~Position.WhiteKing;
        var blackMaterial = Position.BlackPieces & ~Position.BlackKing;

        // Both sides have just kings
        if (whiteMaterial.IsEmpty() && blackMaterial.IsEmpty())
        {
            return true;
        }

        // If white has only its king
        if (whiteMaterial.IsEmpty())
        {
            // Black has exactly one knight
            if (blackMaterial == Position.BlackKnights && blackMaterial.Count() == 1)
            {
                return true;
            }
            // or exactly one bishop
            if (blackMaterial == Position.BlackBishops && blackMaterial.Count() == 1)
            {
                return true;
            }
        }

        // If black has only its king
        if (blackMaterial.IsEmpty())
        {
            // White has exactly one knight
            if (whiteMaterial == Position.WhiteKnights && whiteMaterial.Count() == 1)
                return true;
            // or exactly one bishop
            if (whiteMaterial == Position.WhiteBishops && whiteMaterial.Count() == 1)
                return true;
        }

        return false;
    }
}
