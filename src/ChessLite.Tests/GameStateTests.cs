using ChessLite.Parsing;

namespace ChessLite.Tests;

public class GameStateTests
{
    [Test]
    public async Task GetDrawState_WhenHalfmoveClockIsOneHundred_ReturnsDrawByFiftyMoveRule()
    {
        var game = new Game(Fen.Parse("4k3/8/8/8/8/8/8/4K3 w - - 100 1"));

        var state = game.GetDrawState();

        await Assert.That(state).IsEqualTo(GameState.DrawByFiftyMoveRule);
    }

    [Test]
    public async Task GetDrawState_WhenPositionRepeatsThreeTimes_ReturnsDrawByRepetition()
    {
        var game = new Game(Fen.Parse("4k1n1/8/8/8/8/8/8/1N2K3 w - - 0 1"));

        game.MakeUciMove("b1d2");
        game.MakeUciMove("g8f6");
        game.MakeUciMove("d2b1");
        game.MakeUciMove("f6g8");
        game.MakeUciMove("b1d2");
        game.MakeUciMove("g8f6");
        game.MakeUciMove("d2b1");
        game.MakeUciMove("f6g8");

        var state = game.GetDrawState();

        await Assert.That(state).IsEqualTo(GameState.DrawByRepetition);
    }

    [Test]
    public async Task GetDrawState_WhenMaterialIsInsufficient_ReturnsDrawByInsufficientMaterial()
    {
        var game = new Game(Fen.Parse("4k3/8/8/8/8/8/8/4K3 w - - 0 1"));

        var state = game.GetDrawState();

        await Assert.That(state).IsEqualTo(GameState.DrawByInsufficientMaterial);
    }

    [Test]
    public async Task GetDrawState_WhenCheckmate_ReturnsOngoing()
    {
        var game = new Game(Fen.Parse("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1"));

        var state = game.GetDrawState();

        await Assert.That(state).IsEqualTo(GameState.Ongoing);
    }
}
