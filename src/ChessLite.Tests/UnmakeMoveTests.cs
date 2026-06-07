using ChessLite.Parsing;
using ChessLite.Movement;
using ChessLite.State;

namespace ChessLite.Tests;

public class UnmakeMoveTests
{
    [Test]
    public async Task UnmakeMove_NullMove_RestoresOriginalPosition()
    {
        const string fen = "rnbqkbnr/pppp1ppp/8/4p3/3PP3/8/PPP2PPP/RNBQKBNR b KQkq d3 0 2";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        game.MakeNullMove();
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_WhenNullMoveIsNestedAfterNormalMove_RestoresOriginalPosition()
    {
        var game = new Game();
        var expected = Fen.Format(game);

        game.MakeUciMove("e2e4");
        game.MakeNullMove();
        game.UndoMove();
        game.UndoMove();

        await Assert.That(Fen.Format(game)).IsEqualTo(expected);
    }

    [Test]
    public async Task LegalMoves_WhenBlackKingOnE7AndWhiteBishopAttacksD7_ExcludesE7D7()
    {
        const string fen = "r1bn1b1r/ppq1kppp/4pn2/1B6/3P4/1QN2N2/PP3PPP/R1B2RK1 b - - 8 11";
        var position = Fen.Parse(fen);
        var game = new Game(position);

        Span<Move> moves = stackalloc Move[218];
        var moveCount = game.WriteLegalMoves(moves);
        var containsIllegalKingMove = false;

        for (var i = 0; i < moveCount; i++)
        {
            containsIllegalKingMove |= moves[i].ToString() == "e7d7";
        }

        await Assert.That(containsIllegalKingMove).IsFalse();
    }

    [Test]
    public async Task LegalMoves_WhenBlockerMovesOffEnemyBishopRay_RefreshesEnemyAttacks()
    {
        const string fen = "r1b2b1r/ppq1kppp/2n1pn2/1B6/3P4/1QN2N2/PP3PPP/R1B2RK1 b - - 8 11";
        var game = new Game(Fen.Parse(fen));

        game.MakeUciMove("c6d8");
        game.MakeNullMove();

        Span<Move> moves = stackalloc Move[218];
        var moveCount = game.WriteLegalMoves(moves);
        var containsIllegalKingMove = false;

        for (var i = 0; i < moveCount; i++)
        {
            containsIllegalKingMove |= moves[i].ToString() == "e7d7";
        }

        await Assert.That(containsIllegalKingMove).IsFalse();
    }

    [Test]
    public async Task UnmakeMove_KingsideCastlingWhite_RestoresOriginalPosition()
    {
        // Set up a position with only the white king on e1 and a white rook on h1.
        const string fen = "3k4/8/8/8/8/8/8/4K2R w K - 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        // White castling kingside (e1→g1)
        game.MakeUciMove("e1g1");
        game.UndoMove();

        // After unmaking, the position should match the starting state.
        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_QueensideCastlingWhite_RestoresOriginalPosition()
    {
        // Set up a position with the white king on e1 and a white rook on a1.
        const string fen = "3k4/8/8/8/8/8/8/R3K3 w Q - 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        // White queenside castling (e1→c1)
        game.MakeUciMove("e1c1");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_KingsideCastlingBlack_RestoresOriginalPosition()
    {
        // Set up a position with the black king on e8 and a black rook on h8.
        const string fen = "4k2r/8/8/8/8/8/8/4K3 b k - 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        // Black kingside castling: e8 → g8.
        game.MakeUciMove("e8g8");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_QueensideCastlingBlack_RestoresOriginalPosition()
    {
        // Set up a position with the black king on e8 and a black rook on a8.
        const string fen = "r3k3/8/8/8/8/8/8/4K3 b q - 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        // Black queenside castling: e8 → c8.
        game.MakeUciMove("e8c8");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_DoublePawnPushWhite_RestoresOriginalPosition()
    {
        // Using the standard starting position:
        // A double pawn push from e2 to e4 should set en passant to e3.
        var position = new Position();
        var expected = new Position();
        var game = new Game(position);

        game.MakeUciMove("e2e4");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_DoublePawnPushBlack_RestoresOriginalPosition()
    {
        // In the standard starting position, white moves first.
        // After a dummy white move, a black pawn from e7 moves to e5,
        // which should set the en passant target to e6.
        var position = new Position();
        var game = new Game(position);
        game.MakeUciMove("a2a3"); // White non-interfering move.

        // Capture the state after white's move as the expected state for black's move.
        var expected = new Position();
        var expectedGame = new Game(expected);

        expectedGame.MakeUciMove("a2a3");

        game.MakeUciMove("e7e5");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    [Arguments("q")]
    [Arguments("r")]
    [Arguments("b")]
    [Arguments("n")]
    public async Task UnmakeMove_PromotionWhite_RestoresOriginalPosition(string promo)
    {
        // Create a position with a white pawn on g7 (ready to promote) and a black king.
        const string fen = "3k4/6P1/8/8/8/8/8/3K4 w - - 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        var move = $"g7g8{promo}";
        game.MakeUciMove(move);
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    [Arguments("q")]
    [Arguments("r")]
    [Arguments("b")]
    [Arguments("n")]
    public async Task UnmakeMove_PromotionBlack_RestoresOriginalPosition(string promo)
    {
        // Create a position with a black pawn on a2 (ready to promote) and a white king.
        const string fen = "3k4/8/8/8/8/8/p7/3K4 b - - 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        var move = $"a2a1{promo}";
        game.MakeUciMove(move);
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_EnPassantWhite_RestoresOriginalPosition()
    {
        // Set up a position where a white pawn on d5 can capture en passant a black pawn on e5.
        const string fen = "4k3/8/8/3Pp3/8/8/8/4K3 w - e6 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        game.MakeUciMove("d5e6");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_EnPassantBlack_RestoresOriginalPosition()
    {
        // Set up a position where a black pawn on d4 can capture en passant a white pawn on e4.
        const string fen = "4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        game.MakeUciMove("d4e3");
        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }

    [Test]
    public async Task UnmakeMove_WhenKnightCaptures_RestoresOriginalPosition()
    {
        // Set up a position where a white knight on f3 captures a black pawn on e5.
        const string fen = "rnbqkbnr/ppp2ppp/8/3pp3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 0 3";
        var position = Fen.Parse(fen);
        var expected = Fen.Parse(fen);
        var game = new Game(position);

        game.MakeUciMove("f3e5");
        var intermediatePosition = Fen.Parse("rnbqkbnr/ppp2ppp/8/3pN3/4P3/8/PPPP1PPP/RNBQKB1R b KQkq - 0 3");
        await Assert.That(position).IsEquivalentTo(intermediatePosition);

        game.UndoMove();

        await Assert.That(position).IsEquivalentTo(expected);
    }
}
