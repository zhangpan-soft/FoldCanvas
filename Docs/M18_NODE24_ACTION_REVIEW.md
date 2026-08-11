# M18 Node 24 Action review

Review date: 2026-08-12

This record binds each executable GitHub Action reference to primary evidence
from its official repository. Tags are review labels; the lowercase full commit
SHA is the workflow execution identity. All selected commits reported a valid
GitHub commit signature when queried through the GitHub REST API.

## Selected releases

| Action | Release | Full commit SHA | `runs.using` | Decision |
| --- | --- | --- | --- | --- |
| `actions/checkout` | [`v7.0.1`](https://github.com/actions/checkout/releases/tag/v7.0.1) | `3d3c42e5aac5ba805825da76410c181273ba90b1` | `node24` | Upgrade |
| `actions/upload-artifact` | [`v7.0.1`](https://github.com/actions/upload-artifact/releases/tag/v7.0.1) | `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` | `node24` | Upgrade |
| `actions/download-artifact` | [`v8.0.1`](https://github.com/actions/download-artifact/releases/tag/v8.0.1) | `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c` | `node24` | Upgrade |
| `game-ci/unity-test-runner` | [`v4.3.1`](https://github.com/game-ci/unity-test-runner/releases/tag/v4.3.1) | `0ff419b913a3630032cbe0de48a0099b5a9f0ed9` | `node20` | Retain bounded exception |

The release tag refs for all four rows resolved directly to the listed commit
SHAs. The selected first-party `action.yml` files declare Node 24. Their current
release metadata and the exact commit pages are:

- [checkout commit](https://github.com/actions/checkout/commit/3d3c42e5aac5ba805825da76410c181273ba90b1)
- [upload-artifact commit](https://github.com/actions/upload-artifact/commit/043fb46d1a93c77aae656e7c1c64a875d1fc6a0a)
- [download-artifact commit](https://github.com/actions/download-artifact/commit/3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c)
- [unity-test-runner commit](https://github.com/game-ci/unity-test-runner/commit/0ff419b913a3630032cbe0de48a0099b5a9f0ed9)

## Compatibility review

- All FoldCanvas jobs use GitHub-hosted `ubuntu-latest`; none depends on a
  self-hosted runner below the Node 24 minimum runner version documented by the
  artifact Actions.
- Checkout v7 blocks unsafe fork-PR source by default for
  `pull_request_target` and `workflow_run`. FoldCanvas's credential-bearing
  `pull_request_target` workflow deliberately has no checkout. The sole
  workflow-run checkout is gated to a successful same-repository scheduled or
  manually dispatched candidate-soak run.
- Upload-artifact v7 adds optional direct upload, but FoldCanvas does not set
  `archive: false`; existing archive behavior and artifact names remain.
- FoldCanvas downloads artifacts by name, never a single `artifact-ids` value,
  so the earlier artifact-ID path migration does not apply.
- Download-artifact v8 makes digest mismatch an error by default. This is
  consistent with FoldCanvas's fail-closed evidence policy. No suppression or
  compatibility override is added.

No events, permissions, job names, fork guards, secrets, artifact names,
artifact paths, retention periods, Unity versions, or evidence thresholds need
to change for these upgrades.

## No tagged GameCI Node 24 release

The upstream [Node 24 PR #304](https://github.com/game-ci/unity-test-runner/pull/304)
merged as commit `08fd329f00a18efa297140b14ac28ebce742759e` on
2026-06-20, and the official `main` branch now declares `node24`. However, the
latest release and latest version tag are still `v4.3.1`, whose `action.yml`
declares `node20`.

Therefore M18 retains signed release commit
`0ff419b913a3630032cbe0de48a0099b5a9f0ed9`. A floating `main` ref, an
unreleased commit presented as a version, an unreviewed fork, and warning
suppression are all rejected. This exception ends only when GameCI publishes an
official tagged Node 24 release that passes the same upstream and hosted review.

## Verification boundary

The selected implementation head `d3ed7eb25fbc52f99580ef99b26b59d351702754`
passed the hosted matrix on 2026-08-12:

- [PR Unity run 31533100145](https://github.com/zhangpan-soft/FoldCanvas/actions/runs/31533100145):
  477 passed, zero failed/skipped/inconclusive; repeated clean archive installs,
  producer/receiver handoff, and source-first upgrade all succeeded.
- [M13 run 31533018780](https://github.com/zhangpan-soft/FoldCanvas/actions/runs/31533018780):
  477 passed plus 512 deterministic robustness cases with zero unexpected
  cases; XML, Editor log, reports, replay evidence, and validation were
  uploaded as artifact `9117850962`.
- [Public qualification run 31533891950](https://github.com/zhangpan-soft/FoldCanvas/actions/runs/31533891950):
  exact public assets, two public consumers, RC2-to-stable source upgrade, and
  stable-publication binding passed. Proof artifact `9118098988` reports
  `qualified: true` and the exact public archive SHA-256.

Log audit found no Node 20 runtime warning in jobs that used only the selected
first-party Actions. In each job that also invoked GameCI, GitHub's final
runtime warning named only
`game-ci/unity-test-runner@0ff419b913a3630032cbe0de48a0099b5a9f0ed9`.

The closeout commit changes only milestone documentation. It must still pass
the protected required checks and exact-head audit before merge. That does not
replace or weaken the successful implementation-head long-run and public
qualification evidence above.
