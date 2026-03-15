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
    public async Task Parse_IntoExistingGame_ResetsHistoryAndPosition()
    {
        var game = new Game();
        game.MakeUciMove("e2e4");

        const string pgn = "1. d4 d5";

        var parsed = PgnParser.Parse(pgn, game);

        await Assert.That(ReferenceEquals(game, parsed)).IsTrue();
        await Assert.That(Fen.Format(game)).IsEqualTo("rnbqkbnr/ppp1pppp/8/3p4/3P4/8/PPP1PPPP/RNBQKBNR w KQkq d6 0 2");
        await Assert.That(game.GetMoveHistory().Count()).IsEqualTo(2);
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
        game.MakeUciMove("e2e4");
        game.MakeUciMove("e7e5");
        game.MakeUciMove("g1f3");
        game.MakeUciMove("b8c6");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 e5 2. Nf3 Nc6 *");
    }

    [Test]
    public async Task Format_Checkmate_AppendsHashAndResult()
    {
        var game = new Game();
        game.MakeUciMove("f2f3");
        game.MakeUciMove("e7e5");
        game.MakeUciMove("g2g4");
        game.MakeUciMove("d8h4");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. f3 e5 2. g4 Qh4# 0-1");
    }

    [Test]
    public async Task Format_Castling_UsesCastleNotation()
    {
        var game = new Game();
        game.MakeUciMove("g1f3");
        game.MakeUciMove("g8f6");
        game.MakeUciMove("g2g3");
        game.MakeUciMove("g7g6");
        game.MakeUciMove("f1g2");
        game.MakeUciMove("f8g7");
        game.MakeUciMove("e1g1");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. Nf3 Nf6 2. g3 g6 3. Bg2 Bg7 4. O-O *");
    }

    [Test]
    public async Task Format_EnPassant_UsesCaptureNotation()
    {
        var game = new Game();
        game.MakeUciMove("e2e4");
        game.MakeUciMove("a7a6");
        game.MakeUciMove("e4e5");
        game.MakeUciMove("d7d5");
        game.MakeUciMove("e5d6");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 a6 2. e5 d5 3. exd6 *");
    }

    [Test]
    public async Task Format_Promotion_UsesPromotionSuffix()
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
        game.MakeUciMove("b7a8q");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. a4 h5 2. a5 h4 3. a6 h3 4. axb7 hxg2 5. bxa8=Q *");
    }

    [Test]
    public async Task Format_Check_UsesPlusSuffix()
    {
        var game = new Game();
        game.MakeUciMove("e2e4");
        game.MakeUciMove("e7e5");
        game.MakeUciMove("d1h5");
        game.MakeUciMove("b8c6");
        game.MakeUciMove("h5f7");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 e5 2. Qh5 Nc6 3. Qxf7+ *");
    }

    [Test]
    public async Task Format_DisambiguatesSameTargetRookMove()
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
        game.MakeUciMove("a1e1");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. e4 e5 2. Nf3 Nf6 3. Bc4 Bc5 4. O-O O-O 5. Nc3 a6 6. d3 a5 7. Bd2 a4 8. Qe2 a3 9. Rae1 *");
    }

    [Test]
    public async Task Format_DisambiguatesSameTargetKnightMoveByRank()
    {
        var game = new Game();
        game.MakeUciMove("g1f3");
        game.MakeUciMove("a7a6");
        game.MakeUciMove("f3d4");
        game.MakeUciMove("a6a5");
        game.MakeUciMove("d4b5");
        game.MakeUciMove("h7h6");
        game.MakeUciMove("b1c3");

        var pgn = Pgn.Format(game);

        await Assert.That(pgn).IsEqualTo("1. Nf3 a6 2. Nd4 a5 3. Nb5 h6 4. N1c3 *");
    }
}
