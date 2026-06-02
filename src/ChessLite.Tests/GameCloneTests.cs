using ChessLite.Parsing;

namespace ChessLite.Tests;

public class GameCloneTests
{
    [Test]
    public async Task Clone_WhenMoveMadeOnClone_DoesNotMutateOriginal()
    {
        var original = new Game();
        original.MakeUciMove("e2e4");

        var clone = original.Clone();
        clone.MakeUciMove("e7e5");

        await Assert.That(Fen.Format(original)).IsEqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");
        await Assert.That(Fen.Format(clone)).IsEqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2");
    }

    [Test]
    public async Task Clone_WhenUndoMove_RestoresCloneOnly()
    {
        var original = new Game();
        original.MakeUciMove("e2e4");
        original.MakeUciMove("e7e5");

        var clone = original.Clone();
        clone.UndoMove();

        await Assert.That(Fen.Format(original)).IsEqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2");
        await Assert.That(Fen.Format(clone)).IsEqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");
    }

    [Test]
    public async Task Clone_WhenPositionRepeated_PreservesRepetitionState()
    {
        var original = new Game(Fen.Parse("4k1n1/8/8/8/8/8/8/1N2K3 w - - 0 1"));
        original.MakeUciMove("b1d2");
        original.MakeUciMove("g8f6");
        original.MakeUciMove("d2b1");
        original.MakeUciMove("f6g8");
        original.MakeUciMove("b1d2");
        original.MakeUciMove("g8f6");
        original.MakeUciMove("d2b1");
        original.MakeUciMove("f6g8");

        var clone = original.Clone();

        await Assert.That(clone.GetDrawState()).IsEqualTo(GameState.DrawByRepetition);
    }
}
