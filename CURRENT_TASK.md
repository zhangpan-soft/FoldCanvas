# Current task

Execute **M17: stable `1.0.0` general availability**.

Authoritative task file:
[`Codex/M17_STABLE_GA.md`](Codex/M17_STABLE_GA.md)

M16 candidate-pinned soak is complete. The exact RC2 candidate remains commit
`4db988ffac6dad4362d126001e5c9a67081ef2b7`, public tag
`v1.0.0-rc.2`, and archive SHA-256
`72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`.

Two genuine scheduled runs (`31076024500` and `31356841867`) each passed
472/472 Edit Mode tests, 512/512 deterministic robustness cases, and 5/5
resource envelopes with zero failed, skipped, or inconclusive tests.

After the 168-hour threshold, evaluator run
[`31501082596`](https://github.com/zhangpan-soft/FoldCanvas/actions/runs/31501082596)
completed at `2026-08-11T14:21:46Z`. Its downloaded artifact
`9105046023` matched GitHub digest
`945b75d662c0eb39395eae03b1ec32cefd8e2d6d8b2a83f759c1aa7c3dfc37b0`.
The report SHA-256 is
`c581d89bb45a6269d183ff73d881d450f20c49e0dbd565e679ac9a922f779ad4`
and records:

- status `ready`;
- 172.5/168 soak hours;
- 2/2 qualifying scheduled runs;
- 14/14 required gates;
- zero open release blockers;
- exact target version `1.0.0`.

M17 converts that qualified lineage into a real stable package. It advances
only release/compatibility identity and evidence: no geometry, topology,
FoldScript, Runtime API shape, dependency, or Unity-version behavior may
change. Stable publication must use a dedicated PR, exact-head audit, protected
`main`, exact `v1.0.0` tag, deterministic archive, and post-publication public
asset plus clean-consumer verification. RC2 remains immutable rollback.

The maintainer may plan, implement, audit, merge, publish the GitHub stable
release, and continue the roadmap autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and external marketplace
publication remain owner escalation points.
