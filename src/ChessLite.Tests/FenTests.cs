using ChessLite.Movement;
using ChessLite.Parsing;
using ChessLite.State;

namespace ChessLite.Tests;

public class FenTests
{
    [Test]
    public async Task Write_Position_ReturnsFullFen()
    {
        const string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var position = Position.ParseFen(fen);

        var writtenFen = Fen.Write(position);

        await Assert.That(writtenFen).IsEqualTo(fen);
    }

    [Test]
    public async Task ParseFen_DoesNotOverflow()
    {
        var position = Position.ParseFen(Constants.StartingPosition);

        await Assert.That(position.WhitePawns.IsEmpty()).IsFalse();
    }

}
