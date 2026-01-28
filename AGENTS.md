# ChessLite Agent Guidelines

## Overview

ChessLite is a high-performance chess library for .NET 8.0+ that provides fast move generation, legal move validation, and game state management using modern chess engine data structures.

## Build, Test, and Run Commands

### Building
```bash
# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release --no-restore

# Build in Debug mode
dotnet build --configuration Debug
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with Release configuration
dotnet test --configuration Release --no-build

# Run a specific test file (TUnit framework)
dotnet test --project src/ChessLite.Tests/ChessLite.Tests.csproj --filter "ClassName~MoveGenerationTests"

# Run a single test method
dotnet test --filter "MethodName~GenerateLegalMoves_InitialPosition_Returns20Moves"
```

### Benchmarking
```bash
# Run benchmarks (using justfile)
just bench

# Or directly
dotnet run --project src/ChessLite.Benchmarks/ChessLite.Benchmarks.csproj --configuration Release
```

## Project Structure

```
src/
├── ChessLite/              # Main library
│   ├── Movement/           # Move generation, attack tables, legality checking
│   ├── Parsing/            # FEN parser and helpers
│   ├── Primitives/         # Core types (Bitboard, Square, PieceType, etc.)
│   └── State/              # Position and Zobrist hashing
├── ChessLite.Tests/        # TUnit test suite (targets .NET 10.0)
└── ChessLite.Benchmarks/   # BenchmarkDotNet performance tests
```

## Code Style Guidelines

### General Formatting
- **Indentation**: 4 spaces for C# files, 2 spaces for project/config files
- **Line endings**: LF (Unix-style)
- **Encoding**: UTF-8
- **Final newline**: Required in all files
- **Trailing whitespace**: Trim from all lines

### C# Language Features
- **Target framework**: .NET 8.0 (main library), .NET 10.0 (tests)
- **Language version**: Latest
- **Nullable reference types**: Enabled (all types must handle nullability)
- **Implicit usings**: Enabled
- **AOT compatibility**: Required (`IsAotCompatible=true`)

### Naming Conventions
- **Classes/Structs**: PascalCase (e.g., `MoveGeneration`, `Bitboard`)
- **Methods**: PascalCase (e.g., `GenerateLegalMoves`, `MakeMove`)
- **Public properties**: PascalCase (e.g., `WhiteToMove`, `EnPassantTarget`)
- **Private fields**: _camelCase with underscore prefix (e.g., `_moveExecutor`, `_packed`)
- **Internal fields**: camelCase for bitboard properties (e.g., `WhitePieces`, `AllPieces`)
- **Constants**: PascalCase for public, PascalCase for internal (e.g., `StartingPosition`)
- **Local variables**: camelCase (e.g., `moveCount`, `kingSquare`)
- **Parameters**: camelCase (e.g., `position`, `isWhite`)

### Types and Structs
- Prefer `readonly struct` for immutable value types (e.g., `Move`, `Bitboard`)
- Use `ref` returns for performance-critical scenarios (e.g., `GetPieceBitboard`)
- Use `Span<T>` and `stackalloc` for stack-allocated arrays to avoid heap allocations
- Leverage implicit conversions where appropriate (e.g., `Bitboard` ↔ `ulong`)

### Performance Patterns
- Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for hot-path methods
- Prefer `Span<T>` over arrays for move generation (e.g., `Span<Move> moves = stackalloc Move[218]`)
- Use bitwise operations extensively for board representation
- Avoid allocations in move generation and evaluation code
- Internal visibility for implementation details exposed to tests/benchmarks via `InternalsVisibleTo`

### Documentation
- **XML comments**: Required for all public APIs
- Use `<summary>`, `<param>`, `<returns>`, `<remarks>` tags appropriately
- Include examples in class-level documentation where helpful
- Document bit layouts and data structures clearly (see `Move._packed` for example)

### Imports Organization
- Rely on implicit usings where possible (System namespace imports)
- Explicit `using` statements for:
  - Project namespaces (e.g., `using ChessLite.Movement;`)
  - Third-party packages (e.g., `using TUnit`)
- Order: System namespaces first, then third-party, then project namespaces
- Place `namespace` declaration on its own line (file-scoped namespaces)

### Error Handling
- Use exceptions for truly exceptional conditions (e.g., `InvalidOperationException`)
- Validate input parameters in public APIs
- Prefer returning sentinel values (e.g., `Square.None`, `PieceType.None`) over nulls where appropriate
- Document exception conditions in XML comments

### Testing Conventions (TUnit)
- Test class names: `{FeatureName}Tests` (e.g., `MoveGenerationTests`, `PerftTests`)
- Test method names: `{MethodName}_{Scenario}_{ExpectedBehavior}` 
  - Example: `GenerateLegalMoves_InitialPosition_Returns20Moves`
- Use `[Test]` attribute for test methods
- Arrange-Act-Assert pattern with comments separating sections
- Use `await Assert.That()` for assertions (TUnit's async assertion pattern)
- Use `stackalloc` for move arrays in tests when performance matters

### Constants and Magic Numbers
- Define constants in `Constants.cs` for shared values
- Use bitboard masks for board positions (e.g., `FileA`, `SecondRank`)
- Document magic numbers inline when they represent chess-specific values (e.g., 218 max moves, 64 squares)

### Special Considerations
- **Bitboard representation**: Little-endian rank-file mapping (A1=0, H8=63)
- **Move encoding**: Packed into 32-bit integer for efficiency (see `Move` struct documentation)
- **FEN parsing**: Support standard FEN notation
- **UCI protocol**: Use UCI move notation (e.g., "e2e4", "e7e8q")
- **Zobrist hashing**: Used for position repetition detection and transposition tables

## Common Patterns

### Creating a new game
```csharp
var game = new Game();  // Starting position
var game = Game.FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
```

### Move generation
```csharp
Span<Move> moves = stackalloc Move[218];  // Max possible moves in any position
int count = MoveGeneration.GenerateLegalMoves(position, moves);
```

### Bitboard operations
```csharp
var occupied = position.WhitePieces | position.BlackPieces;
var isEmpty = bitboard.IsEmpty();
var popCount = bitboard.Count();
```

## Repository Information
- **License**: MIT
- **Author**: Kyle Hickman
- **Repository**: https://github.com/kjhickman/ChessLite
- **Package**: Available on NuGet
