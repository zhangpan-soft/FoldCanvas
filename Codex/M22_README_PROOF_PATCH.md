# M22: proof-first README patch

## Visible proof

1. The English and Chinese README heroes show the maintained 2D cup/sphere
   source beside the real compiled textured and topology results.
2. Every image links to the M21 provenance manifest and reproducible Unity
   command instead of acting as unaudited marketing art.
3. A tracked `1280 x 640` repository social-preview candidate is composed only
   from those validated proof pixels.
4. The package advances to `1.0.1` because both root READMEs are part of the
   deterministic UPM archive; published `v1.0.0` and RC2 assets remain
   immutable.

## Goal

Complete GitHub issue #26's second gate without changing geometry, topology,
FoldScript `0.1`, dependencies, or public Runtime API shape.

## Scope

- proof-first README integration in both maintained languages;
- byte-identical `Documentation~/ProofGallery` image copies so the UPM archive
  README never points at release-excluded `Docs/` paths;
- a release-excluded social-preview candidate and deterministic compositor;
- explicit `1.0.1` package/compiler/changelog/API/corpus version evidence;
- M21 closeout and M22 task/plan records;
- full repository, archive, Unity, clean-install, handoff, upgrade, proof, and
  exact-head hosted validation.

## Non-goals

- publishing a GitHub release, tag, social-preview setting, registry package,
  or external marketplace listing;
- modifying the compiler, geometry, topology, UVs, FoldScript, source samples,
  render shaders, dependencies, Unity version, or public Runtime API shape;
- regenerating or editing the six M21 Unity proof PNGs;
- issue #21 Windows evidence or later geometry work.

## Acceptance

- README hero states `2D canvas + FoldScript -> deterministic 3D geometry` and
  makes cup/sphere source-result-topology relationships visible near the top;
- alt text is specific and local links resolve;
- the social candidate is exactly `1280 x 640`, deterministic, and uses no
  pixels outside the six validated M21 PNGs plus fixed labels/background;
- proof provenance continues to identify Unity `6000.3.20f1`, the maintained
  generator command, sources, geometry hashes, and validation values;
- the immutable M21 manifest remains anchored to package `1.0.0`; hosted
  regeneration records current package `1.0.1` in two independent clean hosts
  and must reproduce the same source, geometry, topology, tool, and per-run
  pixel evidence in both hosts;
- the frozen M21 macOS PNGs and package copies remain byte-identical; hosted
  Linux raster bytes are compared only to a second Linux regeneration because
  GPU raster output is not a cross-platform byte-identity contract;
- package/runtime/changelog/API/corpus version headers agree on `1.0.1` while
  normalized Runtime API shape and all six corpus case identities remain
  unchanged;
- two release builds are byte-identical and produce a new reviewed hash;
- full Unity Edit Mode tests and all clean consumer gates pass before merge.

## Architecture boundary

The M21 images and M22 social candidate are derived documentation evidence.
Only the 2D canvases and FoldScript remain geometry source. README integration
changes package bytes but never feeds pixels or a generated Mesh back into the
compiler.

## Implementation status

Planning started from merged M21 commit
`67fb659ff53e13f21e464b8a4e837b72bdc60c50`. No external CLAIM or open pull
request existed at start. Hosted main repository and Unity runs were green.

Local implementation is complete. The full static/archive/install/upgrade
matrix passes. Unity `6000.3.20f1` installed the final deterministic archive
`56699767ff482e7bfd4a8fcf90e5d2eb5b8ca8d553cf6a2387e5f29f03a4e656`
and passed 478/478 Edit Mode tests with zero failures, skips, or inconclusive
results. Hosted exact-head checks, maintainer audit, and merge remain.

Hosted review of head `9dd3966f5564c23c29c83f77dd960258edbefca7`
exposed an invalid cross-platform assertion: Linux Unity reproduced the exact
source hashes, geometry hashes, and topology values but not the macOS PNG
bytes. M22 now keeps the six M21/macOS bytes frozen while requiring two clean
Linux regenerations to match each other byte-for-byte.
