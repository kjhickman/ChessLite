using ChessLite.Movement;
using ChessLite.Primitives;
using ChessLite.State;

namespace ChessLite.Tests;

public class MakeMoveTests
{
    [Test]
    public async Task MakeMove_CastlingKingsideWhite_UpdatesKingAndRookPositions()
    {
        // Set up a position with only the white king on e1 and a white rook on h1
        const string fen = "3k4/8/8/8/8/8/8/4K2R w K - 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();

        // White castling kingside (e1→g1)
        executor.MakeMove(position, "e1g1");

        // After castling, the king should be on g1 and the rook on f1
        await Assert.That(position.WhiteKing.GetFirstSquare()).IsEqualTo(Square.g1);
        await Assert.That(position.WhiteRooks).IsEqualTo(Square.f1.ToMask());
        await Assert.That(position.CastlingRights).IsEqualTo(CastlingRights.None);
    }

    [Test]
    public async Task MakeMove_CastlingQueensideWhite_UpdatesKingAndRookPositions()
    {
        // Set up a position with the white king on e1 and a white rook on a1
        const string fen = "3k4/8/8/8/8/8/8/R3K3 w Q - 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();

        // White queenside castling (e1→c1)
        executor.MakeMove(position, "e1c1");

        // After castling, the king should be on c1 and the rook should move from a1 to d1
        await Assert.That(position.WhiteKing.GetFirstSquare()).IsEqualTo(Square.c1);
        await Assert.That(position.WhiteRooks).IsEqualTo(Square.d1.ToMask());
        await Assert.That(position.CastlingRights).IsEqualTo(CastlingRights.None);
    }

    [Test]
    public async Task MakeMove_CastlingKingsideBlack_UpdatesKingAndRookPositions()
    {
        // Set up a position with the black king on e8 and a black rook on h8.
        const string fen = "4k2r/8/8/8/8/8/8/4K3 b k - 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();

        // Black kingside castling: e8 -> g8.
        executor.MakeMove(position, "e8g8");

        // After castling, the king should be on g8 and the rook on f8.
        await Assert.That(position.BlackKing.GetFirstSquare()).IsEqualTo(Square.g8);
        await Assert.That(position.BlackRooks).IsEqualTo(Square.f8.ToMask());
        await Assert.That(position.CastlingRights).IsEqualTo(CastlingRights.None);
    }

    [Test]
    public async Task MakeMove_CastlingQueensideBlack_UpdatesKingAndRookPositions()
    {
        // Set up a position with the black king on e8 and a black rook on a8.
        const string fen = "r3k3/8/8/8/8/8/8/4K3 b q - 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();

        // Black queenside castling: e8 -> c8.
        executor.MakeMove(position, "e8c8");

        // After castling, the king should be on c8 and the rook on d8.
        await Assert.That(position.BlackKing).IsEqualTo(Square.c8.ToMask());
        await Assert.That(position.BlackRooks).IsEqualTo(Square.d8.ToMask());
        await Assert.That(position.CastlingRights).IsEqualTo(CastlingRights.None);
    }

    [Test]
    public async Task MakeMove_DoublePawnPushWhite_SetsEnPassantTarget()
    {
        // Using the standard starting position:
        // A double pawn push from e2 to e4 should set the en passant target to e3.
        var position = new Position();
        var executor = new MoveExecutor();
        executor.MakeMove(position, "e2e4");

        await Assert.That(position.WhitePawns.Intersects(Square.e2)).IsFalse();
        await Assert.That(position.WhitePawns.Intersects(Square.e4)).IsTrue();
        await Assert.That(position.EnPassantTarget).IsEqualTo(Square.e3);
    }

    [Test]
    public async Task MakeMove_DoublePawnPushBlack_SetsEnPassantTarget()
    {
        // In the standard starting position white moves first.
        // After a dummy white move, black pawn from e7 can move to e5.
        // The double pawn push should set the en passant target to e6.
        var position = new Position();
        var executor = new MoveExecutor();
        executor.MakeMove(position, "a2a3"); // White makes a non-interfering move.
        executor.MakeMove(position, "e7e5");

        await Assert.That(position.BlackPawns.Intersects(Square.e7)).IsFalse();
        await Assert.That(position.BlackPawns.Intersects(Square.e5)).IsTrue();
        await Assert.That(position.EnPassantTarget).IsEqualTo(Square.e6);
    }

    [Test]
    [Arguments("q", PromotedPieceType.Queen)]
    [Arguments("r", PromotedPieceType.Rook)]
    [Arguments("b", PromotedPieceType.Bishop)]
    [Arguments("n", PromotedPieceType.Knight)]
    public async Task MakeMove_PromotionWhite_UpdatesPiece(string promo, PromotedPieceType expectedType)
    {
        // Create a position with a white pawn on g7 (ready to promote) and a black king.
        const string fen = "3k4/6P1/8/8/8/8/8/3K4 w - - 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();
        var move = $"g7g8{promo}";
        executor.MakeMove(position, move);

        // The pawn should be removed from g7 and the promoted piece placed on g8.
        await Assert.That(position.WhitePawns.Intersects(Square.g7)).IsFalse();

        switch (expectedType)
        {
            case PromotedPieceType.Queen:
                await Assert.That(position.WhiteQueens.Intersects(Square.g8)).IsTrue();
                break;
            case PromotedPieceType.Rook:
                await Assert.That(position.WhiteRooks.Intersects(Square.g8)).IsTrue();
                break;
            case PromotedPieceType.Bishop:
                await Assert.That(position.WhiteBishops.Intersects(Square.g8)).IsTrue();
                break;
            case PromotedPieceType.Knight:
                await Assert.That(position.WhiteKnights.Intersects(Square.g8)).IsTrue();
                break;
        }
    }

    [Test]
    [Arguments("q", PromotedPieceType.Queen)]
    [Arguments("r", PromotedPieceType.Rook)]
    [Arguments("b", PromotedPieceType.Bishop)]
    [Arguments("n", PromotedPieceType.Knight)]
    public async Task MakeMove_PromotionBlack_UpdatesPiece(string promo, PromotedPieceType expectedType)
    {
        // Create a position with a black pawn on a2 (ready to promote) and a white king.
        const string fen = "3k4/8/8/8/8/8/p7/3K4 b - - 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();
        var move = $"a2a1{promo}";
        executor.MakeMove(position, move);

        // The pawn should be removed from a2 and the promoted piece placed on a1.
        await Assert.That(position.BlackPawns.Intersects(Square.a2)).IsFalse();

        switch (expectedType)
        {
            case PromotedPieceType.Queen:
                await Assert.That(position.BlackQueens.Intersects(Square.a1)).IsTrue();
                break;
            case PromotedPieceType.Rook:
                await Assert.That(position.BlackRooks.Intersects(Square.a1)).IsTrue();
                break;
            case PromotedPieceType.Bishop:
                await Assert.That(position.BlackBishops.Intersects(Square.a1)).IsTrue();
                break;
            case PromotedPieceType.Knight:
                await Assert.That(position.BlackKnights.Intersects(Square.a1)).IsTrue();
                break;
        }
    }

    [Test]
    public async Task MakeMove_EnPassantWhite_CapturesOpponentPawn()
    {
        // Set up a position where a white pawn on d5 can capture en passant a black pawn on e5
        const string fen = "4k3/8/8/3Pp3/8/8/8/4K3 w - e6 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();

        // White en passant move: d5 -> e6
        executor.MakeMove(position, "d5e6");

        // After en passant, the white pawn should now be on e6
        // and the black pawn on e5 should be captured.
        await Assert.That(position.WhitePawns.Intersects(Square.e6)).IsTrue();
        await Assert.That(position.BlackPawns.Intersects(Square.e5)).IsFalse();
    }

    [Test]
    public async Task MakeMove_EnPassantBlack_CapturesOpponentPawn()
    {
        // Set up a position where a black pawn on d4 can capture en passant a white pawn on e4.
        const string fen = "8/8/8/8/3pP3/8/8/8 b - e3 0 1";
        var position = new Position(fen);
        var executor = new MoveExecutor();

        // Black en passant move: d4 -> e3.
        executor.MakeMove(position, "d4e3");

        // After en passant, the black pawn should now be on e3
        // and the white pawn on e4 should be captured.
        await Assert.That(position.BlackPawns & Bitboard.Mask(Square.e3)).IsNotEqualTo(0UL);
        await Assert.That(position.WhitePawns & Bitboard.Mask(Square.e4)).IsEqualTo(0UL);
    }
}
