# Goal

Deliver M13 on `codex/m13-robustness-scale`: establish a deterministic,
bounded robustness and scale evidence layer that finds compiler exceptions,
unstable diagnostics or geometry, stale state, unsafe growth, and retry defects
without changing FoldCanvas source ownership or adding geometry behavior.

# User-visible proof

Run `Tools > FoldCanvas > Run M13 Robustness Smoke` twice and obtain the same
semantic digest for a versioned replayable corpus. Interrupt one run between
cases and confirm that no project source, Mesh, scene, selection, or prior
complete report changed; retry and obtain the clean digest. In hosted CI, run a
larger corpus under Unity `6000.3.20f1` and retain XML, Editor log, canonical
report, resource observations, and exact replay identities for failures.

# Scope

- explicit fixed-algorithm deterministic generator and versioned case identity
- bounded valid/invalid property suites across existing M00-M12 source surface
- canonical source, geometry, diagnostic, validation, and aggregate hashing
- maintained near-limit and large deterministic fixtures
- repeated compilation and fresh-state retry evidence
- Editor-only cancellable-between-cases robustness runner
- atomic complete-report persistence under `Library/FoldCanvas`
- conservative reviewed elapsed-time and allocation envelopes
- replay records for unexpected exception/result/hash/limit outcomes
- focused Edit Mode tests, complete package regression, PR smoke job, and
  scheduled/manual long-run workflow
- documentation, CI scripts, package version, compatibility evidence, and
  newest-first changelog entry required by the accepted implementation

# Non-goals

- new panels, operations, seam modes, geometry equations, or topology behavior
- random global state or nondeterministic case generation
- compiler parallelization or changing deterministic output order
- thread abort, native-code sandboxing, or guaranteed mid-compile cancellation
- automatic source minimization, repair, remesh, cleanup, bevel, subdivision,
  smoothing, or CSG
- raising safety limits merely to make a benchmark succeed
- Runtime filesystem/network behavior or a new package dependency
- public API/FoldScript 1.0 freeze, release-candidate declaration, external
  marketplace publication, or M14 implementation

# Files expected to change

- `CURRENT_TASK.md`, `Codex/M13_ROBUSTNESS_SCALE.md`, this plan, roadmap, and
  English/Chinese project documentation
- Editor-only robustness case generator, runner, report contracts, replay
  writer, menu command, and optional test-only fault/cancellation seam
- M13 Edit Mode test fixtures and canonical baseline/report data
- repository validation and deterministic evidence scripts
- GitHub Actions smoke and scheduled/manual long-run jobs
- `package.json`, Runtime version constant, public API/production corpus only if
  a reviewed version change requires their existing locked values to advance
- `CHANGELOG.md` newest entry first

# Geometry invariants

- Appearance canvas plus canonical FoldScript remain the only authoritative
  source. Robustness reports, replay files, hashes, timings, allocations,
  screenshots, XML, logs, and Meshes are derived evidence.
- The harness calls the same public compiler path as ordinary sources. It does
  not inject triangles, repair topology, or treat a generated Mesh as input.
- Identical case identity and package/compiler/FoldScript/generator versions
  produce identical canonical source, compile settings, validation level,
  ordered diagnostics, and semantic geometry evidence.
- Valid cases produce only finite positions, normals, and UVs; in-range ordered
  indices; preserved source UV/provenance; and successful requested validation.
- Invalid or over-budget cases return the expected stable root diagnostic and
  no Mesh. They never silently lower tessellation, skip an operation, or emit
  approximate success.
- Every case uses a fresh source instance and compiler result. A failed,
  cancelled, or large case cannot influence the next case.
- Runtime stays free of UnityEditor, filesystem/network behavior, global random
  state, fuzz infrastructure, and new dependencies.

# Determinism, boundary, and tolerance decisions

- Case IDs use `(generatorVersion, suiteId, seed, ordinal)` with ordinal string
  comparison for suite IDs and a repository-owned unsigned 64-bit generator.
- Generated numerical values are drawn from documented finite buckets around
  zero, sign changes, angular limits, tessellation limits, weld epsilon, and
  scale extremes; arbitrary NaN/Infinity inputs belong only to explicit invalid
  buckets.
- Full-turn winding, boundary direction, seam selection, Solidify topology,
  sphere/torus frames, and validation tolerances remain exactly those documented
  by M00-M12. M13 asserts them; it does not redefine them.
- Semantic evidence includes canonical source, ordered diagnostics with
  structured values, complete ordered geometry/topology hashes, validation
  evidence, and source-before/source-after hashes.
- Environment metadata and time/allocation observations are outside the
  semantic digest. Their reviewed envelope IDs and pass/fail outcomes are
  included so threshold changes remain explicit.

# Implementation steps

1. Close M12 with exact PR/run/artifact evidence, merge PR #11, synchronize
   `main`, create the M13 branch/specification/roadmap entry, and replace the
   active plan before code changes.
2. Inventory M00-M12 public source domains, stable diagnostics, geometry limits,
   existing fixtures, M10 performance evidence, and M11/M12 clean-host gates.
3. Define the fixed generator algorithm, versioned case/replay/report contracts,
   canonical semantic projection, bounded suite sizes, and explicit failure
   classifications.
4. Implement in-memory case generation for finite valid and intentionally
   invalid panels, references, operations, compile settings, and existing
   geometry families without touching project assets.
5. Implement result inspection and complete semantic hashing for source
   non-mutation, finite buffers, ordered indices/UVs/provenance, diagnostics,
   validation, topology, and closed-volume evidence where applicable.
6. Add maintained scale fixtures for one-below/exact/one-over vertex, triangle,
   FoldScript, validation, and handoff limits plus large valid compound assets.
7. Implement the Editor runner, between-case cancellation, fresh-state retry,
   atomic complete-report replacement, and replay evidence under `Library/`.
8. Measure current Unity `6000.3.20f1` time/allocation evidence, choose generous
   reviewed envelopes that catch gross regressions, and document environment
   limitations rather than turning timing noise into geometry behavior.
9. Add focused Edit Mode tests, run the complete M00-M13 suite, and verify
   failure/cancellation leaves source, scene, selection, project assets, and the
   last complete report unchanged.
10. Add repository checks plus required PR smoke and scheduled/manual long-run
    Unity jobs with non-empty XML/log/report/replay artifacts.
11. Advance the preview package version and all locked compatibility/corpus/API
    evidence together, update the newest-first changelog and bilingual docs,
    then run JSON/asmdef/repository/release/static checks.
12. Create a draft PR, audit exact hosted artifacts and semantic evidence,
    record the maintainer self-audit, and merge only with green checks and no
    unresolved security, compiler, determinism, topology, or installation issue.

# Test matrix

## Generator and replay

- same identity -> byte-identical canonical source and settings
- shuffled execution order -> unchanged per-case result
- different suite/seed/ordinal -> distinct stable identity
- generator version present in every report and replay record
- no global Unity or .NET random-state dependence

## Valid and invalid properties

- bounded valid cases compile without exception and produce finite, indexed,
  deterministic geometry with preserved source UV/provenance
- bounded invalid cases never throw, never return Mesh, never mutate source,
  and return deterministic diagnostic code/order/context
- failures retain exact canonical replay identity and classification
- tolerance-edge cases repeat across independent source instances

## Scale and limits

- vertex and triangle one-below/exact cases succeed as specified
- one-over cases return FC5005/FC5006 with no Mesh or partial build state
- checked arithmetic overflow returns FC5007 deterministically
- FoldScript, Strict validation, archive, and decoded-canvas limits retain their
  stable diagnostics and do not allocate unbounded payloads
- maintained large valid assets retain counts, hashes, topology, and requested
  validation evidence

## Repetition, cancellation, and retry

- repeated small/large compiles do not change source or geometry hashes
- failed large compile followed by small valid compile equals clean small compile
- cancellation between cases writes no project asset or partial final report
- retry after cancellation equals clean complete run
- last complete report survives cancellation and injected persistence failure
- no scene object, selection, hidden preview, Mesh, or Material leak

## Resource evidence and CI

- warmup and measured iteration counts are explicit
- semantic digest excludes raw elapsed/allocation observations
- envelope identity and pass/fail are deterministic and reviewed
- local smoke, hosted PR smoke, and larger scheduled/manual report use the same
  generator/version and can replay every failure
- XML and Editor logs prove Unity actually started and exited after tests

## Regression

- every existing M00-M12 Edit Mode test remains enabled
- repository validation, deterministic package build, JSON parsing, Python
  compilation, asmdef checks, and `git diff --check` pass
- clean archive installation and M12 producer/receiver evidence remain intact

# Risks and rollback

- **Flaky timing gate:** warm up, use scenario-specific generous envelopes,
  store raw observations outside semantic hashes, and change thresholds only in
  reviewed baseline diffs.
- **Fuzzer nondeterminism:** own the integer algorithm and generator version;
  never use framework/global random state or unordered iteration.
- **Corpus too expensive:** retain a required bounded PR smoke set and move the
  larger count to scheduled/manual CI without weakening replayability.
- **Exception hidden by harness:** record and fail unexpected exceptions; do not
  convert them into compiler success or a generic public diagnostic.
- **Cancellation overclaim:** check only between synchronous cases and before
  atomic report replace; document that one compile is not forcibly aborted.
- **Scale OOM:** pre-compute checked counts, use explicit case caps, and exercise
  true package maxima only in an isolated reviewed scenario if safe.
- **User scratch contamination:** stage explicit paths only and keep all
  untracked `Project~` scenes/results outside commits.
- Rollback is reverting isolated M13 commits. M12 remains merged as `0d4a576`.

# Progress log

- 2026-08-04: Verified PR #11 exact head `5edcc23e1e98e785c5f76802f45f19962569aa49`.
  Repository run `30838162209` and Unity run `30838161788` were green; hosted
  XML reported 431/431 complete package tests and 1/1 producer plus 1/1 receiver.
- 2026-08-04: Downloaded artifacts matched GitHub digests, Unity logs identified
  `6000.3.20f1`, local/hosted package and handoff SHA-256 values matched, and the
  independently recomputed producer/receiver comparison was byte-identical.
- 2026-08-04: Recorded a maintainer self-audit for exact head `5edcc23`, marked
  PR #11 ready, merged it into `main` as `0d4a576`, and fast-forwarded local
  `main` without adding or changing the user's untracked `Project~` scratch.
- 2026-08-04: Created `codex/m13-robustness-scale`, the M13 specification,
  active roadmap status, and this execution plan. No M13 implementation or M14
  release decision was made in the planning commit.
- 2026-08-04: Began M13 implementation with a fixed SplitMix64 generator and
  four replayable smoke suites covering planar/Roll success plus the existing
  FC3022 Roll tessellation and FC3011 off-grid Fold failures. The first report
  slice hashes canonical source, complete compiled geometry, ordered
  diagnostics, source-before/source-after state, and finite buffers; raw timing
  and cancellation are deliberately not part of this slice.
- 2026-08-04: Unity `6000.3.20f1` passed 6/6 focused M13 tests and 437/437
  complete package tests in a fresh archive-installed project. The default
  64-case smoke passed twice with zero unexpected results, semantic SHA-256
  `fad8385cf02227371df0128b213f38e0b7962cf49ea968b3f2109d03b2ac0290`,
  and byte-identical report SHA-256
  `a0a8a57c29b2bd3a769c87de33d6f6bdbad14422defdd706ef304bd306c1ed9a`.
  Reports resolved under the active project `Library/FoldCanvas`; the user's
  tracked and untracked `Project~` source/scratch files were not written.
- 2026-08-04: Added the first maintained M13 scale fixture through the ordinary
  compiler. A `127 x 63` rectangle plus Solidify produces 17,904 render
  vertices, 16,384 logical topology vertices, and 32,764 triangles with locked
  SHA-256 `2045705d501770fc866354e431c58462e1ddee8f278d3fa738ce03b6e24b47b8`.
  Exact vertex and triangle limits pass; lowering either relevant limit by one
  returns deterministic `FC5005` or `FC5006` without a Mesh. A separate native
  355-panel Strict source returns `FC5019` at candidate pair 250,001, and a
  failed large compile does not change a following small replay. Unity
  `6000.3.20f1` passed all 7 focused scale/retry tests in an independent
  archive-installed host, followed by the complete 444/444 M00-M13 Edit Mode
  suite with zero failures, skips, or inconclusive results.
- 2026-08-04: Added cooperative cancellation immediately before each
  synchronous robustness case and before final report replacement. Cancelled
  runs stay incomplete and cannot replace the last report; complete reports use
  sibling temporary files plus atomic replacement, and injected persistence
  failure removes the temporary file while preserving prior bytes. Retry after
  cancellation matches a clean serialized report. Unity `6000.3.20f1` passed
  all 5 focused cancellation/retry/persistence tests in an independent
  archive-installed host and then passed the complete 449/449 M00-M13 Edit Mode
  suite with zero failures, skips, or inconclusive results.
- 2026-08-04: Added maintained large fixtures for planar tessellation, a Strict
  welded and solidified cup, sixteen stitched sphere gores, a Strict doubly
  welded torus, and unequal boundaries resampled to 1,025 Stitch samples. Each
  locks complete render/topology/triangle counts and geometry SHA-256 evidence
  across independent source instances. During the cup probe, thickness greater
  than the authored wall-row spacing was confirmed to fold the first inner
  strip back across the next strip; the accepted fixture keeps thickness below
  that spacing, while a paired Strict test deterministically returns `FC5018`
  without a Mesh for the invalid case. Unity `6000.3.20f1` passed all 6 focused
  family-scale tests and the complete 455/455 M00-M13 suite in an independent
  archive-installed host with zero failures, skips, or inconclusive results.
- 2026-08-04: Added a reviewed five-scenario resource envelope document and an
  Editor runner that performs fresh-source warmups/measurements, verifies exact
  geometry evidence, records elapsed time plus managed allocation without
  placing raw noise in the semantic hash, and atomically persists only complete
  derived reports. In an independent archive-installed Unity `6000.3.20f1`
  host, all 3 focused resource-contract tests and the complete 458/458 M00-M13
  suite passed with zero failures, skips, or inconclusive results. The default
  one-warmup/three-measurement run passed 5/5 envelopes; observed medians were
  1,086.986 ms / 51,352,602 bytes planar, 1,628.242 ms / 75,051,662 bytes cup,
  290.962 ms / 99,468,724 bytes sphere, 143.354 ms / 24,902,865 bytes torus,
  and 64.729 ms / 14,558,998 bytes Stitch. Each reviewed ceiling is 10 seconds
  and 512 MiB, and the selected method was Unity's
  `GC Allocated In Frame` profiler counter.

# Decisions made

- M13 is an evidence and hardening milestone, not a new geometry milestone.
- A repository-owned fixed integer generator plus explicit version/seed/ordinal
  is required because framework random algorithms and global state are not a
  durable replay contract.
- The pull-request corpus stays bounded; a larger scheduled/manual run supplies
  long-running evidence without making every small change wait for exhaustive
  cases.
- Cancellation is cooperative between synchronous cases. Mid-compile thread
  abort would risk corrupted Unity/managed state and is outside this milestone.
- Time and allocation measurements are derived environmental evidence. Only
  explicit envelope outcomes join the canonical report; raw noise never affects
  compiler results or semantic hashes.
- Failure replay lives outside project source and cannot become a generated
  Mesh escape hatch. The canonical source remains the only route back into the
  compiler.
- M13 scale gates use lower explicit per-case caps rather than allocating up to
  the one-million/two-million production defaults. The fixture first proves its
  checked final count at the exact cap, then changes only that cap by one so the
  stable operation-level diagnostic cannot be confused with source
  tessellation preflight.
- Resource thresholds are deliberately broad gross-regression alarms, not
  performance promises. Raw observations and the selected measurement adapter
  stay outside semantic identity, while the baseline hash, measurement
  availability, exact geometry, and pass/fail outcomes remain reviewable gates.

# Final verification

The deterministic smoke, budget/Strict scale, multi-family scale, repeated
compile, failure-then-retry, cooperative cancellation/atomic-report, and
resource-envelope slices are implemented and locally verified. Hosted long-run
evidence, package-version advancement, and the final PR audit remain pending.
The final M13 audit must record:

- exact branch head, package/compiler/FoldScript/generator versions
- generator algorithm/version, suite IDs, seeds, case counts, and semantic hash
- focused M13 and complete M00-M13 Unity totals with XML/log paths
- near-limit counts and stable diagnostics for each rejected boundary
- cancellation/retry/source non-mutation and no-partial-report evidence
- time/allocation envelope IDs, observed values, and environment
- required PR smoke plus scheduled/manual long-run IDs and artifact digests
- clean-install and M12 handoff regression results
- repository/release/static validation results and `git diff --check`
- remaining manual foreground checks
- explicit statement that no new geometry behavior or M14/1.0 work was included
