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
- Protect `main` after the first collaborators join.
- Require the repository-checks workflow before merge.
- Use squash merge or rebase merge to keep geometry changes reviewable.
- Require pull requests to include deterministic tests for new geometry behavior.
- Do not accept generated Unity caches or baked meshes as source-of-truth changes.

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
