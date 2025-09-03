# Tasks for Explainer: Async Ask API for AkkaUrlManager and AkkaHost Performance Parity

Parent Explainer: Explainer: Async Ask API for AkkaUrlManager and AkkaHost Performance Parity

## Parent Checklist

- [x] Implement async APIs on AkkaUrlManager and keep sync shims — #(replace-with-issue)
- [x] Replace sync-over-async Ask usage with awaited Ask + WaitAsync and surface timeouts/cancellation — #(replace-with-issue)
- [x] Add unit tests for async success, timeout, and cancellation (Windows x64) — #(replace-with-issue)
- [ ] Validate AkkaHost performance parity vs Console and tune if needed (?10% delta) — #(replace-with-issue)
- [x] Update docs for maintainers (API notes, timeout behavior, minimal logging) — #(replace-with-issue)

## Relevant Files

- src/AkkaHost/AkkaUrlManager.cs — Add async APIs; use Ask + WaitAsync; XML docs; sync shims preserved
- test/AkkaUrlManagerAsyncTests.cs — Async success, timeout, cancellation tests
- test/UrlConverterPermutations.csproj — Reference AkkaHost for tests
- docs/README.md — Maintainer notes on async APIs and semantics

## Performance Validation Results (Windows x64, .NET 9, Release)

- Console (ConcurrentUrlRepository + IncrementingIntegerUrlShortener, sequential):
  - Read throughput ~1,430,803 ops/sec (N=2)
- AkkaHost (same repo/shortener, sequential):
  - Throughput ~222,918 ops/sec (N=15)
- Delta: ~84% slower than Console (outside ?10% target)

Observations:
- Overhead dominated by per-request Ask temporary actor creation and message round-trips.
- HOCON tuning (throughput=1000), logging minimized, and ThreadPool minimums already configured.

Proposed next steps (requires approval):
- Replace Ask with a pooled, correlation-based reply coordinator:
  - Introduce correlated request/response messages (carry Guid CorrelationId).
  - Add a lightweight ReplyCoordinator actor to map CorrelationId -> TaskCompletionSource without per-request actor.
  - Manager: send Tell with coordinator as sender; enforce timeout via WaitAsync on the TCS.
  - Actors (UrlWriteActor/UrlReadActor): add handlers for correlated messages; reply with correlated responses.
- Expected: remove most Ask allocations, reduce latency, approach Console throughput.

Status: pending approval to implement the coordinator optimization.

