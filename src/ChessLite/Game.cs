using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.State;

namespace ChessLite;

/// <summary>
/// Represents a chess game with move history, position state, and game state tracking.
/// </summary>
public class Game
{
    private readonly MoveExecutor _moveExecutor;
    private readonly ulong[] _repetitionTable;
    private int _currentPly;

    /// <summary>
    /// Gets the current position of the chess game.
    /// </summary>
    public Position Position { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Game"/> class with the starting position.
    /// </summary>
    public Game()
    {
        Position = new Position();
        _moveExecutor = new MoveExecutor();
        _repetitionTable = new ulong[512];
        _currentPly = 0;
    }

    public Game(Position position)
    {
        Position = position;
        _moveExecutor = new MoveExecutor();
        _repetitionTable = new ulong[512];
        _currentPly = 0;
    }

    /// <summary>
    /// Creates a new game from a FEN (Forsyth-Edwards Notation) string.
    /// </summary>
    /// <param name="fen">The FEN string representing the chess position.</param>
    /// <returns>A new <see cref="Game"/> instance with the specified position.</returns>
    public static Game ParseFen(ReadOnlySpan<char> fen)
    {
        return new Game(Position.ParseFen(fen));
    }

    public static bool TryParseFen(ReadOnlySpan<char> fen, out Game? game)
    {
        if (Position.TryParseFen(fen, out var position))
        {
            game = new Game(position!);
            return true;
        }
        game = null;
        return false;
    }

    /// <summary>
    /// Writes all legal moves for the current position to the specified span.
    /// </summary>
    /// <param name="moves">The span to write the legal moves to. Should have a minimum size of 218.</param>
    /// <returns>The number of legal moves written to the span.</returns>
    public int WriteLegalMoves(Span<Move> moves)
    {
        return MoveGeneration.GenerateLegalMoves(Position, moves);
    }

    /// <summary>
    /// Gets an enumerable collection of all legal moves for the current position.
    /// </summary>
    /// <returns>An enumerable collection of legal moves.</returns>
    public IEnumerable<Move> GetLegalMoves()
    {
        var moves = new Move[218];
        var count = MoveGeneration.GenerateLegalMoves(Position, moves);
        for (var i = 0; i < count; i++)
        {
            yield return moves[i];
        }
    }

    /// <summary>
    /// Gets an enumerable collection of all moves made in the game. Ordered from first to last.
    /// </summary>
    /// <returns>An enumerable collection of moves from the game history.</returns>
    public IEnumerable<Move> GetMoveHistory()
    {
        return _moveExecutor.GetMoveHistory();
    }

    /// <summary>
    /// Makes a move on the board and updates the position.
    /// </summary>
    /// <param name="move">The move to make.</param>
    public void MakeMove(Move move)
    {
        _moveExecutor.MakeMove(Position, move);
        _repetitionTable[_currentPly++] = Position.ZobristHash;
    }

    /// <summary>
    /// Parses a move in UCI notation and applies it if it is legal.
    /// </summary>
    /// <param name="uciMove">The UCI move string.</param>
    /// <exception cref="ArgumentException">Thrown when the move is not legal for the current position.</exception>
    public void MakeUciMove(ReadOnlySpan<char> uciMove)
    {
        var parsedMove = Helpers.MoveFromUci(Position, uciMove);

        Span<Move> legalMoves = stackalloc Move[218];
        var count = MoveGeneration.GenerateLegalMoves(Position, legalMoves);

        for (var i = 0; i < count; i++)
        {
            if (legalMoves[i] == parsedMove)
            {
                MakeMove(legalMoves[i]);
                return;
            }
        }

        throw new ArgumentException("Move is not legal in the current position.", nameof(uciMove));
    }

    /// <summary>
    /// Undoes the last move made on the board and restores the previous position.
    /// </summary>
    public void UndoMove()
    {
        _moveExecutor.UndoMove(Position);
        _currentPly--;
    }

    /// <summary>
    /// Resets the game to a new position, clearing the move history and repetition table.
    /// </summary>
    /// <param name="position">The new position to set.</param>
    public void ResetPosition(Position position)
    {
        Position = position;
        _currentPly = 0;
        Array.Clear(_repetitionTable, 0, _repetitionTable.Length);
    }

    /// <summary>
    /// Determines whether the current side to move is in check.
    /// </summary>
    /// <returns><c>true</c> if the current side to move is in check; otherwise, <c>false</c>.</returns>
    public bool IsInCheck()
    {
        var kingSquare = Position.WhiteToMove ? Position.WhiteKing.GetFirstSquare() : Position.BlackKing.GetFirstSquare();
        return MoveGeneration.IsSquareAttacked(Position, kingSquare, byWhite: !Position.WhiteToMove);
    }

    /// <summary>
    /// Gets the current state of the game (ongoing, checkmate, stalemate, or various draw conditions).
    /// </summary>
    /// <returns>The current <see cref="GameState"/> of the game.</returns>
    public GameState GetState()
    {
        // Check for draw by fifty-move rule
        if (Position.HalfmoveClock >= 100)
        {
            return GameState.DrawByFiftyMoveRule;
        }

        // Check for draw by repetition
        var repetitionCount = 0;
        for (var i = 0; i < _currentPly; i++)
        {
            if (_repetitionTable[i] == Position.ZobristHash)
            {
                repetitionCount++;
                if (repetitionCount >= 3)
                {
                    return GameState.DrawByRepetition;
                }
            }
        }

        // Check for draw by insufficient material
        if (IsInsufficientMaterial())
        {
            return GameState.DrawByInsufficientMaterial;
        }

        // Check if there are any legal moves
        Span<Move> moves = stackalloc Move[218];
        var legalMoveCount = MoveGeneration.GenerateLegalMoves(Position, moves);

        if (legalMoveCount == 0)
        {
            // No legal moves - either checkmate or stalemate
            return IsInCheck() ? GameState.Checkmate : GameState.Stalemate;
        }

        return GameState.Ongoing;
    }

    private bool IsInsufficientMaterial()
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
