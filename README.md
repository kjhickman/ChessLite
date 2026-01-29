# ChessLite

A lightweight, high-performance chess library for .NET. ChessLite provides fast move generation, legal move validation, and game state management using modern chess engine data structures and methods.

This library just provides some core chess logic that may be suitable for writing a high-performance chess engine, creating a chess GUI, or even a chess game.

## Requirements

- .NET 8.0 or later

## Example Usage

```csharp
using ChessLite;
using ChessLite.Movement;
using ChessLite.Parsing;

// Create a new game
var game = new Game();

// Or start from a specific position
var position = Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
var gameFromFen = new Game(position);

// You can parse PGN as well
var pgnGame = Pgn.Parse("1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 1-0");

// Get all legal moves
foreach (var move in game.GetLegalMoves())
{
    Console.WriteLine(move);
}

// Or with zero allocations (moves written to buffer)
Span<Move> movesBuffer = stackalloc Move[218];
var moveCount = game.WriteLegalMoves(movesBuffer);

// Make a move using UCI notation
game.MakeUciMove("e2e4");

// Or with short algebraic notation (SAN)
game.MakeSanMove("Nf6");

// Undo the last move
game.UndoMove();

// Format the current position as FEN or PGN
string fen = Fen.Format(game);
string pgn = Pgn.Format(pgnGame);
```

## Acknowledgments

- For the inspiration to start this project, [this video](https://youtu.be/w4FFX_otR-4?si=gOWyYTxIoEBOXrBn) by [Bartek Spitza](https://github.com/bartekspitza)
- The chess programming community at the [Chess Programming Wiki](https://www.chessprogramming.org)
