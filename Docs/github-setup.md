# GitHub setup

`FoldCanvas` is a working project name. Before a public launch, search GitHub, package registries, domains, and relevant trademark databases. Rename the repository, package identifier, namespace, and documentation together if the name is unavailable or creates confusion.

## First publish

From the repository root:

```bash
git init
git add .
git commit -m "chore: bootstrap FoldCanvas"
git branch -M main
git remote add origin <YOUR_GITHUB_REPOSITORY_URL>
git push -u origin main
```

After the repository exists, add its URLs to the optional `documentationUrl`, `changelogUrl`, and `licensesUrl` fields in `package.json`, and add a Discussions link to `.github/ISSUE_TEMPLATE/config.yml` if Discussions is enabled.

## Recommended repository settings

- Enable Issues and Discussions.
- Keep `main` protected before inviting collaborators or external agents.
- Require repository, deterministic package, trusted-contribution, Unity,
  clean-install, handoff, and source-upgrade checks before merge.
- Use squash merge or rebase merge to keep geometry changes reviewable.
- Require pull requests to include deterministic tests for new geometry behavior.
- Do not accept generated Unity caches or baked meshes as source-of-truth changes.
- Treat fork PRs as untrusted proposals. Do not pass Unity credentials to them;
  review the exact diff and land approved work through a maintainer-owned
  integration PR with full privileged CI.

## Suggested labels

```text
area:compiler
area:editor
area:schema
area:diagnostics
area:documentation
geometry-case
good first issue
help wanted
research
breaking-change
```

## First public issues

1. M00 repository health on Windows and macOS.
2. M01 planar rectangle and disk invariants.
3. A minimal source-canvas gallery.
4. Boundary-order visualization.
5. Mathematical review of the FoldScript coordinate conventions.
