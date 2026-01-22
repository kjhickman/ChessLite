using ChessLite.Movement;
using ChessLite.State;

namespace ChessLite.Tests;

public class AttackGenerationTests
{
    [Test]
    [Arguments("8/8/8/3R4/8/8/8/8 w - - 0 1", 14)]
    [Arguments("8/8/3r4/3R4/8/8/8/8 w - - 0 1", 12)]
    [Arguments("3k4/8/3r4/3R4/8/8/8/3K4 w - - 0 1", 12)]
    [Arguments("3k4/8/3r4/3R4/8/8/3K4/8 w - - 0 1", 11)]
    public async Task CalculateRookAttacks_ForWhite_ReturnsCorrectAttacks(string fen, int expected)
    {
        // Arrange
        var position = new Position(fen);
        var allPieces = position.AllPieces;

        // Act
        var whiteRookAttackCount = AttackGeneration.CalculateRookAttacks(position.WhiteRooks, allPieces).Count();

        // Assert
        await Assert.That(whiteRookAttackCount).IsEqualTo(expected);
    }

    [Test]
    [Arguments("8/8/8/4r3/8/8/8/8 w - - 0 1", 14)]
    [Arguments("8/8/8/4r3/4R3/8/8/8 w - - 0 1", 11)]
    [Arguments("4k3/8/8/4r3/4R3/8/8/4K3 w - - 0 1", 11)]
    [Arguments("8/4k3/8/4r3/4R3/8/8/4K3 w - - 0 1", 10)]
    public async Task CalculateRookAttacks_ForBlack_ReturnsCorrectAttacks(string fen, int expected)
    {
        // Arrange
        var position = new Position(fen);
        var allPieces = position.AllPieces;

        // Act
        var whiteRookAttackCount = AttackGeneration.CalculateRookAttacks(position.BlackRooks, allPieces).Count();

        // Assert
        await Assert.That(whiteRookAttackCount).IsEqualTo(expected);
    }

    [Test]
    [Arguments("8/8/8/4B3/8/8/8/8 w - - 0 1", 13)]
    [Arguments("2k5/8/8/3b4/4B3/8/8/3K4 w - - 0 1", 10)]
    [Arguments("2k5/8/8/3b4/4B3/3K4/8/8 w - - 0 1", 8)]
    public async Task CalculateBishopAttacks_ForWhite_ReturnsCorrectAttacks(string fen, int expected)
    {
        // Arrange
        var position = new Position(fen);
        var allPieces = position.AllPieces;

        // Act
        var whiteBishopAttackCount = AttackGeneration.CalculateBishopAttacks(position.WhiteBishops, allPieces).Count();

        // Assert
        await Assert.That(whiteBishopAttackCount).IsEqualTo(expected);
    }

    [Test]
    [Arguments("8/8/8/3b4/8/8/8/8 w - - 0 1", 13)]
    [Arguments("2k5/8/8/3b4/4B3/8/8/3K4 w - - 0 1", 10)]
    [Arguments("8/8/2k5/3b4/4B3/8/8/3K4 w - - 0 1", 8)]
    public async Task CalculateBishopAttacks_ForBlack_ReturnsCorrectAttacks(string fen, int expected)
    {
        // Arrange
        var position = new Position(fen);
        var allPieces = position.AllPieces;

        // Act
        var whiteBishopAttackCount = AttackGeneration.CalculateBishopAttacks(position.BlackBishops, allPieces).Count();

        // Assert
        await Assert.That(whiteBishopAttackCount).IsEqualTo(expected);
    }

    [Test]
    [Arguments("8/8/8/3Q4/8/8/8/8 w - - 0 1", 27)]
    [Arguments("8/1k6/3qB3/3Q4/8/8/3K4/8 w - - 0 1", 21)]
    public async Task CalculateQueenAttacks_ForWhite_ReturnsCorrectAttacks(string fen, int expected)
    {
        // Arrange
        var position = new Position(fen);
        var allPieces = position.AllPieces;

        // Act
        var whiteQueenAttackCount = AttackGeneration.CalculateQueenAttacks(position.WhiteQueens, allPieces).Count();

        // Assert
        await Assert.That(whiteQueenAttackCount).IsEqualTo(expected);
    }

    [Test]
    [Arguments("8/8/8/3q4/8/8/8/8 w - - 0 1", 27)]
    [Arguments("8/1k6/3qB3/3Q4/8/8/3K4/8 w - - 0 1", 18)]
    public async Task CalculateQueenAttacks_ForBlack_ReturnsCorrectAttacks(string fen, int expected)
    {
        // Arrange
        var position = new Position(fen);
        var allPieces = position.AllPieces;

        // Act
        var whiteQueenAttackCount = AttackGeneration.CalculateQueenAttacks(position.BlackQueens, allPieces).Count();

        // Assert
        await Assert.That(whiteQueenAttackCount).IsEqualTo(expected);
    }
}
