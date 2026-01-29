using System.Linq;
using ChessLite.Parsing;

namespace ChessLite.Tests;

public class PgnTests
{
    [Test]
    public async Task Parse_Mainline_ReturnsGameWithHistory()
    {
        const string pgn = "1. e4 e5 2. Nf3 Nc6";

        var game = Pgn.Parse(pgn);

        await Assert.That(Fen.Format(game)).IsEqualTo("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3");
        await Assert.That(game.GetMoveHistory().Count()).IsEqualTo(4);
    }

    [Test]
    public async Task Parse_IgnoresAnnotationsAndComments()
    {
        const string pgn = "1. e4! {comment} (1. d4 d5) e5 $1 2. Nf3?! Nc6 1/2-1/2";

        var game = Pgn.Parse(pgn);

        await Assert.That(Fen.Format(game)).IsEqualTo("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3");
    }

    [Test]
    public async Task Parse_WithFenTag_UsesCustomPosition()
    {
        const string pgn = "[FEN \"8/8/8/8/8/8/4K3/7k w - - 0 1\"]\n\n1. Kf2 *";

        var game = Pgn.Parse(pgn);

        await Assert.That(Fen.Format(game)).IsEqualTo("8/8/8/8/8/8/5K2/7k b - - 1 1");
        await Assert.That(game.GetMoveHistory().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Parse_WhenVariantTagProvided_Throws()
    {
        const string pgn = "[Variant \"Chess960\"]\n\n1. e4 e5";

        var exception = Assert.Throws<NotSupportedException>(() => Pgn.Parse(pgn));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Parse_WhenEcoTagProvided_Throws()
    {
        const string pgn = "[ECO \"C20\"]\n\n1. e4 e5";

        var exception = Assert.Throws<NotSupportedException>(() => Pgn.Parse(pgn));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Parse_WhenMoveIsInvalid_Throws()
    {
        const string pgn = "1. Qz4";

        var exception = Assert.Throws<ArgumentException>(() => Pgn.Parse(pgn));

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Format_Mainline_FormatsMovetextWithResult()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e2e4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e7e5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g1f3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "b8c6"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 e5 2. Nf3 Nc6 *");
    }

    [Test]
    public async Task Format_Checkmate_AppendsHashAndResult()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "f2f3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e7e5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g2g4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "d8h4"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. f3 e5 2. g4 Qh4# 0-1");
    }

    [Test]
    public async Task Format_Castling_UsesCastleNotation()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g1f3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g8f6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g2g3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g7g6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "f1g2"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "f8g7"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e1g1"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. Nf3 Nf6 2. g3 g6 3. Bg2 Bg7 4. O-O *");
    }

    [Test]
    public async Task Format_EnPassant_UsesCaptureNotation()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e2e4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a7a6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e4e5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "d7d5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e5d6"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 a6 2. e5 d5 3. exd6 *");
    }

    [Test]
    public async Task Format_Promotion_UsesPromotionSuffix()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a2a4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "h7h5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a4a5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "h5h4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a5a6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "h4h3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a6b7"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "h3g2"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "b7a8q"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. a4 h5 2. a5 h4 3. a6 h3 4. axb7 hxg2 5. bxa8=Q *");
    }

    [Test]
    public async Task Format_Check_UsesPlusSuffix()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e2e4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e7e5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "d1h5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "b8c6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "h5f7"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 e5 2. Qh5 Nc6 3. Qxf7+ *");
    }

    [Test]
    public async Task Format_DisambiguatesSameTargetRookMove()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e2e4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e7e5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g1f3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g8f6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "f1c4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "f8c5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e1g1"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "e8g8"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "b1c3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a7a6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "d2d3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a6a5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "c1d2"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a5a4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "d1e2"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a4a3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a1e1"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 e5 2. Nf3 Nf6 3. Bc4 Bc5 4. O-O O-O 5. Nc3 a6 6. d3 a5 7. Bd2 a4 8. Qe2 a3 9. Rae1 *");
    }

    [Test]
    public async Task Format_DisambiguatesSameTargetKnightMoveByRank()
    {
        var game = new Game();
        game.MakeMove(Helpers.MoveFromUci(game.Position, "g1f3"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a7a6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "f3d4"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "a6a5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "d4b5"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "h7h6"));
        game.MakeMove(Helpers.MoveFromUci(game.Position, "b1c3"));

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. Nf3 a6 2. Nd4 a5 3. Nb5 h6 4. N1c3 *");
    }
}
