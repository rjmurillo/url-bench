# AGENTS.md

[url-bench agent index]|root: .
|IMPORTANT: Read the linked file before you change its topic. Do not rely on memory.
|code standards, build, tests, commits:{.github/copilot-instructions.md}
|Codacy and security scans:{.github/instructions/codacy.instructions.md}
|Aspire, dotnet-inspect skills:{.agents/skills/}
|CI workflow:{.github/workflows/main.yml}

## CI rules agents must not break

|required check: the main ruleset requires the check `build (ubuntu-latest)`. It comes from the `build` gate job in main.yml.
|keep the gate: do not rename `build`, change its matrix, or drop `if: always()`. Each change renames or hides the required check.
|docs-only PRs: `pull_request` has no `paths-ignore`, so the required check always reports.
|docs filter: the `changes` job skips `compile` when every changed path matches `^docs/` or `\.md$`. The gate then passes.
|filter sync: the `changes` patterns must match the `push` paths-ignore list (`docs/**`, `**.md`). Edit both together.
|matrix: `compile` runs `os: [ubuntu-latest]` with `fail-fast: false`. The `build` gate keeps that single entry for its check name.
|concurrency: one group per workflow, event, and PR number or ref. New runs cancel older ones on `pull_request` and `push` only.
