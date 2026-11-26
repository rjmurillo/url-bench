# AGENTS.md

[url-bench agent index]|root: .
|IMPORTANT: Read the linked file before you change its topic. Do not rely on memory.
|code standards, build, tests, commits:{.github/copilot-instructions.md}
|Codacy and security scans:{.github/instructions/codacy.instructions.md}
|Aspire, dotnet-inspect skills:{.agents/skills/}
|CI workflow:{.github/workflows/main.yml}

## CI rules agents must not break

|required check: the main ruleset requires only `build (ubuntu-latest)`. The `build` gate job in main.yml emits it.
|check name: only the `build` job name and its matrix set that name. Renaming either breaks the required check.
|`if: always()` on `build`: without it, a failed compile skips the gate. A skipped required check counts as passing.
|docs-only PRs: never add `paths-ignore` to `pull_request`. The `changes` job skips `compile` instead, and the gate passes.
|docs filter scope: it runs on `pull_request` only. Push, merge queue, dispatch, and call always compile.
|filter sync: edit the `changes` regex and the push `paths-ignore` list together.
|`changes` needs `pull-requests: read` for `gh api`. If `changes` fails, the gate fails.
|concurrency: one group per workflow, event, and PR number or ref. New runs cancel older ones on `pull_request` and `push` only.
