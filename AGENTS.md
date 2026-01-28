# ChessLite Agent Guidelines

## What is ChessLite?

ChessLite is a high-performance chess library for .NET 8.0+ providing fast move generation, legal move validation, and game state management using modern chess engine data structures (bitboards, magic bitboards, Zobrist hashing).

**Core purpose**: Provide the foundational chess logic needed to build chess engines, GUIs, or games with AOT-compatible, allocation-free move generation.

## Project Structure (WHAT)

```text
src/
├── ChessLite/              # Main library (.NET 8.0, AOT-compatible)
│   ├── Movement/           # Move generation, attack tables, legality checking
│   ├── Parsing/            # FEN parser and helpers
│   ├── Primitives/         # Core types (Bitboard, Square, PieceType, etc.)
│   └── State/              # Position and Zobrist hashing
├── ChessLite.Tests/        # TUnit test suite (.NET 10.0)
└── ChessLite.Benchmarks/   # BenchmarkDotNet performance tests
```

**Key architectural patterns**:

- Bitboards for piece representation (little-endian: A1=0, H8=63)
- Moves packed into 32-bit integers (see `src/ChessLite/Movement/Move.cs:15-26`)
- `Span<T>` and `stackalloc` for zero-allocation move generation

## How to Work on ChessLite (HOW)

### Build & Test Commands

```bash
# Build
dotnet build -c Release

# Run all tests (TUnit framework)
dotnet test -c Release

# Run specific test class
dotnet test --filter "ClassName~MoveGenerationTests" -c Release

# Run single test method
dotnet test --filter "MethodName~GenerateLegalMoves_InitialPosition_Returns20Moves" -c Release

# Benchmarks
just bench  # or: dotnet run --project src/ChessLite.Benchmarks/ChessLite.Benchmarks.csproj -c Release
```

### Code Conventions

**Follow existing patterns** - the codebase is an in-context learning resource. Key conventions:

- **Performance**: Use `Span<T>`, `stackalloc`, `[MethodImpl(AggressiveInlining)]` in hot paths (see `src/ChessLite/Primitives/Bitboard.cs:38-57`)
- **Testing**: Pattern is `{MethodName}_{Scenario}_{ExpectedBehavior}` with TUnit's `await Assert.That()` (see `src/ChessLite.Tests/MoveGenerationTests.cs:10-21`)
- **Allocations**: Move generation must be allocation-free. Use `stackalloc Move[218]` (218 = max legal moves in any position)
**Formatting**: Enforced by `.editorconfig` (4 spaces, LF line endings, UTF-8). Let the editor handle it.
**Documentation**: XML comments required for all public APIs. See `src/ChessLite/State/Position.cs` for examples.

### Important Technical Details

- **Chess-specific constants**: 218 max moves, 64 squares (see `src/ChessLite/Constants.cs`)
- **Bitboard operations**: See `src/ChessLite/Primitives/Bitboard.cs` for common patterns
- **Move construction**: Use static factory methods (e.g., `Move.CreateQuiet`, `Move.CreateCapture`) - see `src/ChessLite/Movement/Move.cs:138-239`

### Finding Information

When you need task-specific guidance, read these files:

- Code style details: `.editorconfig` (formatting rules)
- Project configuration: `src/ChessLite/ChessLite.csproj` (build settings, AOT, nullable)
- Test framework usage: `src/ChessLite.Tests/*.cs` (TUnit patterns)
- Performance patterns: `src/ChessLite/Primitives/Bitboard.cs`, `src/ChessLite/Movement/MoveGeneration.cs`

**Prefer file:line references over code snippets** when explaining patterns to avoid documentation drift.
