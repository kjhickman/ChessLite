using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Tests;

public class SanTests
{
    [Test]
    public async Task Format_SimplePawnMove_ReturnsSan()
    {
        var position = new Game().Position;
        var move = GetLegalMove(position, Square.e2, Square.e4);

        var san = San.Format(position, move);

        await Assert.That(san).IsEqualTo("e4");
    }

    [Test]
    public async Task Format_Checkmate_AppendsHash()
    {
        var game = new Game();
        game.MakeUciMove("f2f3");
        game.MakeUciMove("e7e5");
        game.MakeUciMove("g2g4");
        var position = game.Position.Clone();
        var move = GetLegalMove(position, Square.d8, Square.h4);

        var san = San.Format(position, move);

        await Assert.That(san).IsEqualTo("Qh4#");
    }

    [Test]
    public async Task Format_Castling_ReturnsCastleNotation()
    {
        var game = new Game();
        game.MakeUciMove("g1f3");
        game.MakeUciMove("g8f6");
        game.MakeUciMove("g2g3");
        game.MakeUciMove("g7g6");
        game.MakeUciMove("f1g2");
        game.MakeUciMove("f8g7");
        var position = game.Position.Clone();
        var move = GetLegalMove(position, Square.e1, Square.g1);

        var san = San.Format(position, move);

        await Assert.That(san).IsEqualTo("O-O");
    }

    [Test]
    public async Task Format_Promotion_ReturnsPromotionSuffix()
    {
        var game = new Game();
        game.MakeUciMove("a2a4");
        game.MakeUciMove("h7h5");
        game.MakeUciMove("a4a5");
        game.MakeUciMove("h5h4");
        game.MakeUciMove("a5a6");
        game.MakeUciMove("h4h3");
        game.MakeUciMove("a6b7");
        game.MakeUciMove("h3g2");
        var position = game.Position.Clone();
        var move = GetLegalMove(position, Square.b7, Square.a8, PromotedPieceType.Queen);

        var san = San.Format(position, move);

        await Assert.That(san).IsEqualTo("bxa8=Q");
    }

    [Test]
    public async Task Format_AmbiguousMove_Disambiguates()
    {
        var game = new Game();
        game.MakeUciMove("e2e4");
        game.MakeUciMove("e7e5");
        game.MakeUciMove("g1f3");
        game.MakeUciMove("g8f6");
        game.MakeUciMove("f1c4");
        game.MakeUciMove("f8c5");
        game.MakeUciMove("e1g1");
        game.MakeUciMove("e8g8");
        game.MakeUciMove("b1c3");
        game.MakeUciMove("a7a6");
        game.MakeUciMove("d2d3");
        game.MakeUciMove("a6a5");
        game.MakeUciMove("c1d2");
        game.MakeUciMove("a5a4");
        game.MakeUciMove("d1e2");
        game.MakeUciMove("a4a3");
        var position = game.Position.Clone();
        var move = GetLegalMove(position, Square.a1, Square.e1);

        var san = San.Format(position, move);

        await Assert.That(san).IsEqualTo("Rae1");
    }

    private static Move GetLegalMove(Position position, Square from, Square to, PromotedPieceType promotion = PromotedPieceType.None)
    {
        var game = new Game(position);
        if (game.TryGetLegalMove(from, to, promotion, out var move))
        {
            return move;
        }

        throw new InvalidOperationException("Expected legal move was not found.");
    }
}
