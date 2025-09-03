# AkkaHost async APIs and timeout/cancellation semantics

Scope: Logic and AkkaHost only. Public interfaces (IUrlManager, IUrlManagerSpan) unchanged.

What changed
- AkkaUrlManager now exposes async APIs:
  - Task<string> ShortenAndSaveAsync(string longUrl, CancellationToken ct = default)
  - Task<string> GetOriginalUrlAsync(string shortUrl, CancellationToken ct = default)
- Existing sync methods remain as thin shims over async to preserve API compatibility.
- Ask usage is awaited and constrained with Task.WaitAsync to enforce deterministic timeouts and cancellation.

Timeout and cancellation
- Default timeout: 30 seconds (constructor override supported; must be > TimeSpan.Zero).
- On timeout, async methods throw TimeoutException.
- On cancellation, async methods throw OperationCanceledException.
- Minimal logging: none added in manager by default. Rationale: avoid noise under load; diagnose via exceptions and actor logs if enabled.

Usage guidance
- Prefer the async APIs for non-blocking behavior. The sync methods block by design for legacy call sites.
- For span-based reads (IUrlManagerSpan), the method remains synchronous by design; ref-like types are not await-safe.

Performance notes
- Akka HOCON tuning is retained in Program.cs (throughput = 1000, logging minimized).
- ThreadPool minimum workers raised to reduce ramp-up stalls.
- Messages kept minimal to avoid allocations in hot paths.

Compatibility
- No changes to shortener or repository behavior.
- .NET 8 and .NET 9 supported.
