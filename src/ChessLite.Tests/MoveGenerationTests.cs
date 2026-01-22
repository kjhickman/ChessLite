using ChessLite.Movement;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Tests;

public class GenerateLegalMovesTests
{
    [Test]
    public async Task GenerateLegalMoves_InitialPosition_Returns20Moves()
    {
        // Arrange
        var position = new Position();

        // Act
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);

        // Assert
        await Assert.That(moveCount).IsEqualTo(20);
    }

    [Test]
    public async Task GenerateLegalMoves_PetrovicPosition_Returns218Moves()
    {
        // Arrange
        var position = new Position("R6R/3Q4/1Q4Q1/4Q3/2Q4Q/Q4Q2/pp1Q4/kBNN1KB1 w - - 0 1");

        // Act
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);

        // Assert
        await Assert.That(moveCount).IsEqualTo(218);
    }

    [Test]
    public async Task GenerateLegalMoves_WhenCastlingWouldBeIntoCheck_CannotCastle()
    {
        // Arrange
        var position = new Position("r1bqk2r/ppp2ppp/2np1n2/2b1p3/2B1PP2/2N2N2/PPPP2PP/R1BQK2R w KQkq - 2 6");

        // Act
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);
        var movesArray = moves.ToArray();

        // Assert
        await Assert.That(moveCount).IsEqualTo(37);
        await Assert.That(movesArray).DoesNotContain(x => x.SpecialMoveType == SpecialMoveType.LongCastle);
    }

    [Test]
    public async Task GenerateLegalMoves_WhenInCheckAsWhite_CannotCastle()
    {
        // Arrange
        var position = new Position("r1bqkbnr/pppp1ppp/8/4p3/2B1P3/3P1N2/PPn2PPP/RNBQK2R w KQkq - 0 5");

        // Act
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);
        var movesArray = moves.ToArray();

        // Assert
        await Assert.That(moveCount).IsEqualTo(4);
        await Assert.That(movesArray).DoesNotContain(x => x.SpecialMoveType == SpecialMoveType.ShortCastle);
    }

    [Test]
    public async Task GenerateLegalMoves_WhenPieceIsPinned_PieceCannotMove()
    {
        // Arrange
        var position = new Position("r1bqk1nr/pppp1ppp/2n5/4p3/1b2P3/2NP4/PPP2PPP/R1BQKBNR w KQkq - 3 4");

        // Act
        Span<Move> moves = stackalloc Move[218];
        MoveGeneration.GenerateLegalMoves(position, moves);
        var movesArray = moves.ToArray();

        // Assert
        await Assert.That(movesArray).DoesNotContain(x => x.From == Square.c3);
    }

    [Test]
    public async Task GenerateLegalMoves_WhenEnPassantAvailable_FindsEnPassantMove()
    {
        // Arrange
        var position = new Position("4k3/8/8/3Pp3/8/8/8/4K3 w - e6 0 1");

        // Act
        Span<Move> moves = stackalloc Move[218];
        var moveCount = MoveGeneration.GenerateLegalMoves(position, moves);
        var movesArray = moves.ToArray();

        // Assert
        await Assert.That(moveCount).IsEqualTo(7);
        await Assert.That(movesArray).Contains(x => x.SpecialMoveType == SpecialMoveType.EnPassant);
    }
}
