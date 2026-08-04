# M13: Robustness and scale

## Visible proof

1. Run a deterministic smoke corpus twice in Unity `6000.3.20f1`; both runs
   emit byte-identical semantic evidence for every case.
2. Replay any case by generator version, seed, and ordinal without depending on
   test order, locale, machine time, or random global state.
3. Compile maintained large planar, cup, sphere, torus, Stitch, Solidify, and
   validation fixtures just below their declared safety budgets and obtain the
   expected stable hashes and counts.
4. Move each maintained limit one step beyond its accepted boundary and obtain
   the documented stable diagnostic with no Mesh or partial persisted output.
5. Interrupt the robustness runner between cases, verify that source assets and
   generated project assets remain unchanged, then retry and obtain the same
   complete report as a clean run.
6. Run a larger hosted regression corpus in a real Unity process and upload
   parseable XML, `Editor.log`, the canonical robustness report, and replay
   records for every unexpected result.

## Goal

Turn FoldCanvas robustness from a collection of hand-authored examples into a
repeatable production evidence layer. M13 should expose exceptions,
nondeterminism, stale compiler state, unsafe growth, and retry failures before
users encounter them, while preserving the existing 2D source plus geometry
rules architecture.

## Deterministic case contract

- The generator has an explicit string version and a repository-owned unsigned
  64-bit algorithm. It does not use `System.Random`, Unity random state, current
  time, process identity, or enumeration order.
- A case identity is `(generatorVersion, suiteId, seed, ordinal)`. Replaying
  that tuple produces the same canonical FoldScript, appearance dimensions,
  expected class, compile settings, and validation level.
- Generated inputs remain within explicit source-byte, panel, operation,
  tessellation, and geometry caps. Fuzzing is bounded test construction, not an
  excuse to allocate arbitrary data.
- Valid cases assert successful compilation, finite positions/normals/UVs,
  in-range indices, stable ordered geometry/diagnostic hashes, source
  non-mutation, and the requested validation contract.
- Invalid cases assert one stable root-cause diagnostic when the contract calls
  for one, no Mesh, deterministic structured context, source non-mutation, and
  no unhandled exception.
- The harness catches an unexpected exception only to preserve replay evidence;
  it still fails the run. It must not translate a compiler defect into a public
  success or generic diagnostic.

## Scale and resource contract

- Existing compile limits remain authoritative: default one million generated
  vertices, two million generated triangles, bounded FoldScript input, bounded
  Strict validation, and bounded M12 archive/decode behavior.
- Maintained scale fixtures use lower explicit per-case caps suitable for CI
  while exercising one-below, exact-boundary, and one-over behavior. Expected
  counts are computed with checked arithmetic before allocation.
- Geometry and diagnostic equality are hard deterministic gates. Elapsed time
  and managed allocation are recorded as derived environment-specific evidence
  and compared with conservative reviewed envelopes; they never influence
  geometry or change case order.
- A gross time or allocation envelope violation fails the robustness job with a
  replayable scenario identifier. Normal sub-threshold timing variation does
  not alter canonical semantic evidence.
- The first M13 baseline targets Unity `6000.3.20f1`. M14 decides whether and
  how additional Unity versions become release-qualified.

## Interruption and retry contract

- Compilation remains synchronous and isolated in a fresh build buffer. M13
  does not claim it can preempt arbitrary managed code in the middle of one
  compile.
- The Editor robustness runner accepts cancellation between cases and before
  final report replacement. It mutates no `FoldCanvasAsset`, scene object,
  selection, Mesh asset, or source file.
- A complete report is written by temporary-file plus atomic replace. A
  cancelled or failed run cannot overwrite the last complete report with
  truncated JSON.
- Retry starts from an explicit case identity or suite start, creates fresh
  compiler state, and must match a clean run. A failed large case cannot affect
  the next small valid compile.
- Any failure-producing replay source is derived evidence under `Library/` or a
  CI artifact. It is never silently imported as an authoritative project asset.

## Hosted evidence

- Pull requests run the complete package suite plus a bounded M13 smoke corpus.
- A separate scheduled/manual workflow runs the larger corpus with an explicit
  case count and timeout. The workflow must actually start Unity
  `6000.3.20f1`; missing license or absent XML is failure, not success.
- Required artifacts are non-empty XML, `Editor.log`, canonical robustness
  report, environment metadata, and replay records when failures exist.
- The report records package/compiler/FoldScript/generator versions, Unity and
  platform identity, suite/seed/case counts, semantic aggregate digest,
  per-scenario counts, time/allocation observations, and completion state.
- The semantic report projection excludes wall-clock timestamps and raw timing
  noise so repeated-run determinism can be asserted byte-for-byte.

## Tests

- `RobustnessGenerator_SameIdentityProducesCanonicalSource`
- `RobustnessGenerator_DifferentOrderDoesNotChangeCase`
- `RobustnessSmokeCorpus_RepeatedRunHasStableSemanticDigest`
- `RobustnessSmokeCorpus_ValidCasesProduceFiniteDeterministicGeometry`
- `RobustnessSmokeCorpus_InvalidCasesReturnStableDiagnosticsWithoutMesh`
- `RobustnessSmokeCorpus_DoesNotThrowOrMutateSource`
- `Scale_NearVertexBudgetSucceedsDeterministically`
- `Scale_OneVertexOverBudgetReturnsFC5005WithoutMesh`
- `Scale_NearTriangleBudgetSucceedsDeterministically`
- `Scale_OneTriangleOverBudgetReturnsFC5006WithoutMesh`
- `Scale_StrictValidationBudgetReturnsStableDiagnostic`
- `LargeFailureThenSmallRetryMatchesCleanCompile`
- `RepeatedCompile_DoesNotLeakGeometryStateOrChangeSource`
- `RobustnessRun_CancelBetweenCasesLeavesAssetsAndLastReportUnchanged`
- `RobustnessRun_CancelBeforeReportReplaceLeavesLastReportUnchanged`
- `RobustnessRun_RetryAfterCancellationMatchesCleanRun`
- `RobustnessReport_CanonicalProjectionIsByteStable`
- `RobustnessRun_PersistenceFailureLeavesLastReportUnchanged`
- `RobustnessReport_TimeAndAllocationEnvelopesAreExplicit`
- `LongRunReport_ContainsReplayIdentityForEveryUnexpectedCase`

Exact final test names may be split by subsystem, but none of these behaviors
may be removed or weakened.

## Non-goals

- random or mutation-based production code paths
- nondeterministic parallel compiler execution
- mid-compile thread abort or sandboxing native extension code
- automatic source minimization that changes the failing case
- automatic repair, remesh, cleanup, bevel, subdivision, smoothing, or CSG
- a new panel, operation, seam mode, topology family, or serializer version
- raising existing safety limits merely to make a scale fixture pass
- runtime filesystem/network behavior or a package dependency
- M14 API/FoldScript freeze, release-candidate declaration, marketplace
  publication, or `1.0.0`

## Acceptance gate

- repository/static checks are green;
- every pre-existing M00-M12 Edit Mode test remains enabled and green;
- focused M13 tests and the bounded smoke corpus are green locally and hosted;
- the larger hosted Unity run completes with real XML/log/report artifacts;
- repeated semantic evidence and all replay identities are deterministic;
- no tracked or untracked `Project~` user scratch is committed;
- the exact PR head receives a recorded maintainer audit before merge;
- the final report states observed test totals, resource envelopes, hosted run
  and artifact identities, manual foreground checks, and that M14 was not
  implemented.
