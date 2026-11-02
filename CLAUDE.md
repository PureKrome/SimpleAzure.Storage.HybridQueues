# SimpleAzure.Storage.HybridQueues

See `.github/copilot-instructions.md` for code style and project conventions, and `.claude/skills/` for testing and review guidance.

## Subagents (`.claude/agents/`)

Delegate to these proactively instead of doing the checks inline:

- `security-auditor`: run after changes touching serialization, blob/queue access, credentials, logging, or `.github/workflows/`, and before releases.
- `nuget-auditor`: run whenever `Directory.Packages.props`, any `*.csproj`, `packages.lock.json`, or `nuget.config` changes, when package-update PRs land, and before releases.

Both are read-only and return a findings report; apply fixes in the main session after reviewing.
