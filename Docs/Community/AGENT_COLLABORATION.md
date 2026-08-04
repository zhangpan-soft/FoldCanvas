# External AI-agent collaboration policy

FoldCanvas welcomes useful work from human developers, AI coding agents, and
human-agent teams. The author does not change the evidence standard.

## Access model

External agents are untrusted contributors and receive no collaborator,
maintain, admin, release, Actions-secret, Unity-license, or local-workspace
access. Their only code path is:

```text
public repository -> fork -> topic branch -> contributor pull request
                  -> read-only checks + exact-diff maintainer review
                  -> maintainer-owned integration branch and pull request
                  -> privileged Unity CI -> exact-head audit -> maintainer merge
```

This fork-only access rule is the primary credential boundary. A workflow
condition in a pull-request branch is defense in depth, not a security boundary
against someone who already has repository write access: a writer could change
that workflow before it runs. FoldCanvas therefore grants no external agent or
bot collaborator/write access. Before any future non-owner writer is added,
Unity credentials must first move behind a protected GitHub Environment with an
owner approval rule or an equivalent base-owned approval mechanism.

The protected `main` branch requires a pull request, current required checks,
resolved conversations, and a maintainer audit. Force pushes and deletion are
disabled. Zero GitHub approval reviews are required because the autonomous
maintainer is currently the only repository maintainer; the public audit
comment and machine evidence remain mandatory.

GitHub does not pass Actions secrets to fork pull requests. FoldCanvas also
does not try to bypass that boundary: privileged Unity jobs are skipped for a
fork, and the required `Trusted contribution qualification` check deliberately
rejects direct merge of that fork PR. This result means “maintainer integration
required”, not “contribution rejected”.

After reviewing the exact fork head and full diff, the maintainer preserves
authorship and the original PR link while importing the approved patch into a
maintainer-owned, repository-owner-authored integration branch. That
integration PR runs the complete
Unity, install, handoff, upgrade, and exact-head audit gates before merge. The
original fork PR is then closed with the landing commit or a concrete rejection
reason.

The trust check uses `pull_request_target` only to compare repository and
PR-author metadata
from the protected base workflow. It never checks out, imports, evaluates, or
executes fork content, never invokes another Action, and uses a zero-permission
workflow token with no repository secret. No other workflow may use privileged
`pull_request_target` checkout behavior to run an external patch.

Credentialed Unity jobs accept only a repository-owner-authored same-repository
PR or a post-merge push to protected `main`. Feature-branch pushes, forks,
Dependabot, and other bot-authored PRs cannot enter those jobs under the current
fork-only access model.

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
credential boundaries. A fork head is first reviewed as untrusted input; the
maintainer-owned integration head then supplies complete privileged CI
artifacts. A later push to either reviewed head invalidates its prior decision.
External popularity, agent reputation, or the number of generated comments
never substitutes for evidence.
