---
name: profiling
description: Use when profiling ChessLite or other .NET code with dotnet-trace and pvanalyze, collecting .nettrace files, or analyzing CPU stacks, call trees, GC, JIT, allocations, events, exceptions, or SpeedScope output.
---

# Profiling

Use this skill for .NET performance investigations that need `dotnet-trace` collection or `pvanalyze` analysis of `.nettrace` files. Prefer it when the user mentions profiling, `.nettrace`, `dotnet-trace`, `pvanalyze`, CPU stacks, call trees, GC pauses, allocations, JIT cost, exceptions, events, or SpeedScope.

## Setup

Restore repo-local tools before profiling:

```bash
dotnet tool restore
```

Run local tools through the manifest:

```bash
dotnet tool run dotnet-trace -- --version
dotnet tool run pvanalyze -- --help
```

The shorter `dotnet dotnet-trace` and `dotnet pvanalyze` forms may also work from the repo root, but prefer `dotnet tool run ... -- ...` in instructions because it is explicit about using the local manifest.

## Trace Collection

Write traces outside the repo or into ignored artifact folders. For this repo, `/var/folders/jt/7jnzwtdd3ys78ys0jfjl3_sr0000gn/T/opencode` is available for temporary traces.

Collect CPU samples while running a command:

```bash
dotnet tool run dotnet-trace -- collect --output /var/folders/jt/7jnzwtdd3ys78ys0jfjl3_sr0000gn/T/opencode/chesslite.nettrace -- dotnet run --project src/ChessLite.Benchmarks/ChessLite.Benchmarks.csproj --configuration Release --filter "*MakeMove*"
```

Collect from an already running process:

```bash
dotnet tool run dotnet-trace -- collect --process-id <PID> --output /var/folders/jt/7jnzwtdd3ys78ys0jfjl3_sr0000gn/T/opencode/chesslite.nettrace
```

Collect allocation events when allocation-by-type analysis is needed:

```bash
dotnet tool run dotnet-trace -- collect --providers "Microsoft-Windows-DotNETRuntime:0x200001:5" --output /var/folders/jt/7jnzwtdd3ys78ys0jfjl3_sr0000gn/T/opencode/chesslite-alloc.nettrace -- dotnet run --project src/ChessLite.Benchmarks/ChessLite.Benchmarks.csproj --configuration Release
```

Collect verbose runtime events for GC, JIT, exception, and event analysis:

```bash
dotnet tool run dotnet-trace -- collect --providers "Microsoft-Windows-DotNETRuntime:0x4C14FCCBD:5" --output /var/folders/jt/7jnzwtdd3ys78ys0jfjl3_sr0000gn/T/opencode/chesslite-runtime.nettrace -- dotnet run --project src/ChessLite.Benchmarks/ChessLite.Benchmarks.csproj --configuration Release
```

For ChessLite hot-path profiling, prefer a focused harness or a narrowly filtered benchmark scenario. Avoid interpreting profiles dominated by MSBuild, test discovery, BenchmarkDotNet setup, or process startup as library hot paths.

## Analyze With pvanalyze

Always verify the trace first:

```bash
dotnet tool run pvanalyze -- info /path/to/trace.nettrace
```

CPU hotspots:

```bash
dotnet tool run pvanalyze -- cpustacks /path/to/trace.nettrace --top 20
dotnet tool run pvanalyze -- cpustacks /path/to/trace.nettrace --group-by namespace --inclusive
dotnet tool run pvanalyze -- calltree /path/to/trace.nettrace --hot-path
dotnet tool run pvanalyze -- calltree /path/to/trace.nettrace --caller-callee "Game.MakeMove"
```

GC and allocations:

```bash
dotnet tool run pvanalyze -- gcstats /path/to/trace.nettrace
dotnet tool run pvanalyze -- gcstats /path/to/trace.nettrace --timeline
dotnet tool run pvanalyze -- alloc /path/to/trace.nettrace --top 20
```

JIT, exceptions, and events:

```bash
dotnet tool run pvanalyze -- jitstats /path/to/trace.nettrace
dotnet tool run pvanalyze -- exceptions /path/to/trace.nettrace
dotnet tool run pvanalyze -- events /path/to/trace.nettrace --list
dotnet tool run pvanalyze -- events /path/to/trace.nettrace --provider DotNETRuntime --limit 50
```

SpeedScope export:

```bash
dotnet tool run pvanalyze -- cpustacks /path/to/trace.nettrace --format speedscope --output /path/to/trace.speedscope.json
```

Use `--format json` when another tool or agent will consume the result. Use `--from <ms>` and `--to <ms>` only after a baseline command identifies the interesting time window.

## Reporting

When reporting findings, include:

- The exact `dotnet-trace collect` command or the trace file path if the trace already existed.
- The focused `pvanalyze` commands used.
- The process, time window, provider, event type, or method filter when relevant.
- Top exclusive and inclusive CPU costs for CPU investigations.
- Allocation type totals or GC pause/timeline details for memory investigations.
- Any generated artifact paths, such as `.nettrace`, `.speedscope.json`, or `.pvanalyze.etlx` cache files.

## Validation

Use the smallest validation that matches the work:

```bash
dotnet tool restore
dotnet tool run dotnet-trace -- --version
dotnet tool run pvanalyze -- --help
dotnet tool run pvanalyze -- info /path/to/trace.nettrace
```

If JSON output is used, ensure it parses before relying on it for conclusions.
