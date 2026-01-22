# ChessLite

A lightweight, high-performance chess library for .NET. ChessLite provides fast move generation, legal move validation, and game state management using modern chess engine data structures and methods.

This library just provides some core chess logic that may be suitable for writing a high-performance chess engine, creating a chess GUI, or even a chess game.

## Requirements

- .NET 8.0 or later

## Example Usage

```csharp
using ChessLite;
using ChessLite.Movement;

// Create a new game
var game = new Game();

// Or start from a specific position
var game = Game.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

// Get all legal moves
foreach (var move in game.GetLegalMoves())
{
    Console.WriteLine(move);
}

// Make a move using UCI notation
game.MakeUciMove("e2e4");

// Undo the last move
game.UndoMove();
```

## Acknowledgments

- For the inspiration to start this project, [this video](https://youtu.be/w4FFX_otR-4?si=gOWyYTxIoEBOXrBn) by [Bartek Spitza](https://github.com/bartekspitza)
- The chess programming community at the [Chess Programming Wiki](https://www.chessprogramming.org)
