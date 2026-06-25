using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.Primitives;

namespace ChessLite.Tests;

public class GameMoveTests
{
    [Test]
    public async Task MakeSanMove_SimpleMove_UpdatesPosition()
    {
        var game = new Game();

        game.MakeSanMove("e4");

        await Assert.That(game.Position.WhitePawns.Intersects(Square.e2)).IsFalse();
        await Assert.That(game.Position.WhitePawns.Intersects(Square.e4)).IsTrue();
        await Assert.That(game.Position.EnPassantTarget).IsEqualTo(Square.e3);
        await Assert.That(game.Position.WhiteToMove).IsFalse();
    }

    [Test]
    public async Task MakeSanMove_CheckSuffix_UpdatesPositionAndCheck()
    {
        var game = new Game();

        game.MakeUciMove("e2e4");
        game.MakeUciMove("f7f6");

        game.MakeSanMove("Qh5+");

        await Assert.That(game.Position.WhiteQueens.Intersects(Square.h5)).IsTrue();
        await Assert.That(game.IsInCheck()).IsTrue();
        await Assert.That(game.Position.WhiteToMove).IsFalse();
    }

    [Test]
    public async Task MakeSanMove_IllegalMove_ThrowsArgumentException()
    {
        var game = new Game();

        var exception = Assert.Throws<ArgumentException>(() => game.MakeSanMove("e5"));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task MakeSanMove_AmbiguousMove_ThrowsArgumentException()
    {
        var game = new Game(Fen.Parse("4k3/8/8/8/8/8/3N3N/4K3 w - - 0 1"));

        var exception = Assert.Throws<ArgumentException>(() => game.MakeSanMove("Nf3"));

        await Assert.That(exception).IsNotNull();
    }

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

    [Test]
    public async Task MakeUciMove_InvalidPromotionSuffix_ThrowsArgumentException()
    {
        var game = new Game();

        var exception = Assert.Throws<ArgumentException>(() => game.MakeUciMove("g1f3x"));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task WriteLegalMovesFrom_InitialPawn_ReturnsPawnMoves()
    {
        var game = new Game();
        Span<Move> moves = stackalloc Move[4];

        var count = game.WriteLegalMovesFrom(Square.e2, moves);
        var writtenMoves = moves[..count].ToArray();

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(writtenMoves).Contains(x => x.To == Square.e3);
        await Assert.That(writtenMoves).Contains(x => x.To == Square.e4);
    }

    [Test]
    public async Task TryGetLegalMove_LegalMove_ReturnsMove()
    {
        var game = new Game();

        var found = game.TryGetLegalMove(Square.e2, Square.e4, PromotedPieceType.None, out var move);

        await Assert.That(found).IsTrue();
        await Assert.That(move.From).IsEqualTo(Square.e2);
        await Assert.That(move.To).IsEqualTo(Square.e4);
    }

    [Test]
    public async Task TryGetLegalMove_IllegalMove_ReturnsFalse()
    {
        var game = new Game();

        var found = game.TryGetLegalMove(Square.e2, Square.e5, PromotedPieceType.None, out var move);

        await Assert.That(found).IsFalse();
        await Assert.That(move).IsEqualTo(Move.NullMove);
    }

    [Test]
    public async Task TryGetLegalMove_Promotion_MatchesPromotionPiece()
    {
        var game = new Game(Fen.Parse("k7/4P3/8/8/8/8/8/4K3 w - - 0 1"));

        var foundQueen = game.TryGetLegalMove(Square.e7, Square.e8, PromotedPieceType.Queen, out var queenMove);
        var foundNoPromotion = game.TryGetLegalMove(Square.e7, Square.e8, PromotedPieceType.None, out _);

        await Assert.That(foundQueen).IsTrue();
        await Assert.That(queenMove.PromotedPieceType).IsEqualTo(PromotedPieceType.Queen);
        await Assert.That(foundNoPromotion).IsFalse();
    }
}
