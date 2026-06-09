using ChessLite.Parsing;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Tests;

public class PositionTests
{
    [Test]
    public async Task EnumeratePieces_StartingPosition_Returns32Pieces()
    {
        var position = new Position();

        var pieces = position.EnumeratePieces().ToArray();

        await Assert.That(pieces.Length).IsEqualTo(32);
    }

    [Test]
    public async Task EnumeratePieces_StartingPosition_ReturnsPiecesInSquareOrder()
    {
        var position = new Position();

        var pieces = position.EnumeratePieces().ToArray();

        await Assert.That(pieces[0]).IsEqualTo((Square.a1, PieceType.WhiteRook));
        await Assert.That(pieces[1]).IsEqualTo((Square.b1, PieceType.WhiteKnight));
        await Assert.That(pieces[30]).IsEqualTo((Square.g8, PieceType.BlackKnight));
        await Assert.That(pieces[31]).IsEqualTo((Square.h8, PieceType.BlackRook));
    }

    [Test]
    public async Task EnumeratePieces_SparsePosition_DoesNotReturnEmptySquares()
    {
        var position = Fen.Parse("8/8/8/8/3q4/8/4K3/7k w - - 0 1");

        var pieces = position.EnumeratePieces().ToArray();

        await Assert.That(pieces.Length).IsEqualTo(3);
        await Assert.That(pieces[0]).IsEqualTo((Square.h1, PieceType.BlackKing));
        await Assert.That(pieces[1]).IsEqualTo((Square.e2, PieceType.WhiteKing));
        await Assert.That(pieces[2]).IsEqualTo((Square.d4, PieceType.BlackQueen));
    }
}
