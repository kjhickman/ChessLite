# List all available commands
default:
    @just --list --unsorted

# Run benchmarks in release mode
bench:
    dotnet run --project src/ChessLite.Benchmarks/ChessLite.Benchmarks.csproj --configuration Release
