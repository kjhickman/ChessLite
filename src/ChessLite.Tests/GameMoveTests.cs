using ChessLite.Primitives;

namespace ChessLite.Tests;

public class GameMoveTests
{
    [Test]
    public async Task MakeUciMove_LegalMove_UpdatesPosition()
    {
        var game = new Game();

        game.MakeUciMove("e2e4");

        await Assert.That(game.Position.WhitePawns.Intersects(Square.e2)).IsFalse();
        await Assert.That(game.Position.WhitePawns.Intersects(Square.e4)).IsTrue();
        await Assert.That(game.Position.EnPassantTarget).IsEqualTo(Square.e3);
        await Assert.That(game.Position.WhiteToMove).IsFalse();
    }

    [Test]
    public async Task MakeUciMove_IllegalMove_ThrowsArgumentException()
    {
        var game = new Game();

        var exception = Assert.Throws<ArgumentException>(() => game.MakeUciMove("e2e5"));

        await Assert.That(exception).IsNotNull();
    }
}
