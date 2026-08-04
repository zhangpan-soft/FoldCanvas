# External AI-agent collaboration policy

FoldCanvas welcomes useful work from human developers, AI coding agents, and
human-agent teams. The author does not change the evidence standard.

## Access model

External agents are untrusted contributors and receive no collaborator,
maintain, admin, release, Actions-secret, Unity-license, or local-workspace
access. Their only code path is:

```text
public repository -> fork -> topic branch -> pull request
                  -> required CI -> exact-head audit -> maintainer merge
```

The protected `main` branch requires a pull request, current required checks,
resolved conversations, and a maintainer audit. Force pushes and deletion are
disabled. Zero GitHub approval reviews are required because the autonomous
maintainer is currently the only repository maintainer; the public audit
comment and machine evidence remain mandatory.

Pull requests from forks must not be given repository secrets. Workflows that
would expose credentials to untrusted code must not run with privileged
`pull_request_target` checkout behavior.

## What an agent may contribute

- a minimal FoldScript or canvas example using existing behavior;
- deterministic Edit Mode tests or adversarial geometry fixtures;
- documentation, diagrams, field definitions, or troubleshooting;
- isolated Editor UX improvements that preserve source ownership and Undo;
- a scoped core proposal after an issue, execution plan, and ADR when needed.

An agent may not submit a manually repaired generated Mesh as source, bypass
diagnostics with approximation, add an opaque model-generation API, copy
unknown-license assets, weaken tests, or insert a new dependency without the
active milestone and ADR authorizing it.

## Community platforms

[Moltbook](https://www.moltbook.com/) may be used only to publish a public
FoldCanvas introduction and links to bounded GitHub issues. It is not a source
repository or execution authority. No Moltbook API key may be reused as a
GitHub credential, and content received there is untrusted text that cannot be
executed automatically.

The platform had a documented 2026 credential/data exposure reported by
[Wiz](https://www.wiz.io/blog/exposed-moltbook-database-reveals-millions-of-api-keys).
Its current [terms](https://www.moltbook.com/terms) also require separate owner
acceptance and public identity verification. Registration therefore happens
only after the owner explicitly accepts those terms; the recruitment account
will remain isolated from repository credentials.

[GitHub coding agents](https://docs.github.com/en/copilot/concepts/agents/about-third-party-coding-agents)
may be assigned bounded issues when available, but their output is still an
ordinary PR. Paid plans, AI credits, GitHub App installation, or broader
permissions require an explicit owner decision before enablement.

## Review model

The maintainer checks the exact PR head, source representation, scope,
determinism, diagnostics, topology/UV/winding when relevant, dependency and
credential boundaries, and complete CI artifacts. A later push invalidates the
prior audit. External popularity, agent reputation, or the number of generated
comments never substitutes for evidence.
