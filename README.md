# Veloce

A *temporarily* low-performance  chess engine written in C#

## Overview

Veloce aims to be a modern, high-performance chess engine implemented in C#. The engine is compatible with the Universal Chess Interface (UCI) protocol, allowing it to be used with most chess GUIs. The core chess engine will be available as a package for use in your .NET projects!

## Features

- **Bitboard Representation**: Efficient board state management using 64-bit bitboards
- **Alpha-Beta Pruning**: Enhanced search algorithm with alpha-beta pruning
- **Transposition Tables**: Optimize search with cached positions
- **Move Ordering**: Basic ordering of moves to improve pruning efficiency
- **UCI Protocol Support**: Compatible with chess GUI applications
- **Perft Testing**: Comprehensive performance testing for move generation
- **Zobrist Hashing**: Fast position hashing for repetition detection
- **Legal Move Generation**: 100% accurate move generation, proven by extensive perft test suite
- **Evaluation**: Basic positional and material evaluation
- **Native AOT Compatible**: The core engine library is fully Native AOT compatible

## Performance

Veloce is designed with performance as a priority:

- Fast move generation
- Efficient search algorithm with transposition table support
- Optimized bitboard operations for move generation and evaluation
- Low memory footprint with compact state representation

## Requirements

- .NET 8.0 SDK or later
- Compatible with Windows, macOS, and Linux

## Getting Started

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/veloce.git
   cd veloce
   ```

2. Build the project:
   ```bash
   dotnet build -c Release
   ```

3. Run the tests:
   ```bash
   dotnet test
   ```

## Project Structure

The solution is organized into several projects:

- **Veloce**: Core engine library with move generation, evaluation, and search
- **Veloce.Uci**: UCI protocol implementation
- **Veloce.Uci.Lib**: Shared UCI utilities
- **Veloce.Perft**: Performance/correctness testing tool
- **Veloce.SelfPlay**: Temporary self-play testing application
- **Veloce.Tests**: Unit and integration tests
- **Veloce.Benchmarks**: Performance benchmarks

## Roadmap

Future development plans include:

- [ ] Improved evaluation function with more chess knowledge
- [ ] Quiescence search to handle the horizon effect
- [ ] Better move ordering with history heuristics and killer moves
- [ ] Advanced pruning techniques (null move, futility, late move, delta)
- [ ] Parallel search capabilities
- [ ] Opening book support
- [ ] Endgame tablebase integration
- [ ] Time management for tournament play
- [ ] Pondering (thinking on opponent's time)
- [ ] Multi-variant support (Chess960, etc.)
- [ ] NNUE (Efficiently Updatable Neural Network) evaluation
- [ ] Engine vs. engine match testing

## Acknowledgments

- For the inspiration to start this project, [this video](https://youtu.be/w4FFX_otR-4?si=gOWyYTxIoEBOXrBn) by [Bartek Spitza](https://github.com/bartekspitza)
- The chess programming community at the [Chess Programming Wiki](https://www.chessprogramming.org)
