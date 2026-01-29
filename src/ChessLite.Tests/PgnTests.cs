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

        await Assert.That(Fen.Write(game)).IsEqualTo("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3");
        await Assert.That(game.GetMoveHistory().Count()).IsEqualTo(4);
    }

    [Test]
    public async Task Parse_IgnoresAnnotationsAndComments()
    {
        const string pgn = "1. e4! {comment} (1. d4 d5) e5 $1 2. Nf3?! Nc6 1/2-1/2";

        var game = Pgn.Parse(pgn);

        await Assert.That(Fen.Write(game)).IsEqualTo("r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3");
    }

    [Test]
    public async Task Parse_WithFenTag_UsesCustomPosition()
    {
        const string pgn = "[FEN \"8/8/8/8/8/8/4K3/7k w - - 0 1\"]\n\n1. Kf2 *";

        var game = Pgn.Parse(pgn);

        await Assert.That(Fen.Write(game)).IsEqualTo("8/8/8/8/8/8/5K2/7k b - - 1 1");
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
}
