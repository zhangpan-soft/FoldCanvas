# M21: proof-gallery evidence generation

## Visible proof

1. Source-controlled cup images show the real production 2D canvas, the
   maintained textured closed cup, and texture-free closed-volume topology.
2. Source-controlled sphere images show the real eight-gore 2D canvas, the
   maintained textured closed sphere, and its wireframe/seam evidence.
3. A deterministic Unity `6000.3.20f1` batch command regenerates every result
   image from tracked FoldScript/canvas source without editor chrome.
4. A canonical manifest binds every PNG to its source path, generator command,
   source revision, geometry identity, validation values, and SHA-256.
5. Dependency-free repository validation rejects missing, stale, unsafe, or
   unproven gallery evidence.

## Goal

Make FoldCanvas' strongest source-to-result claims directly inspectable while
preserving the immutable public `1.0.0` release and the principle that the 2D
canvas plus FoldScript are authoritative source.

## Scope

- GitHub issue #26, gate one only;
- clean Unity batch rendering from maintained cup and sphere sources;
- release-excluded assets under `Docs/Community/ProofGallery/`;
- deterministic provenance, validation, and hosted evidence;
- M20 task-record completion.

## Non-goals

- editing `README.md` or `README.zh-CN.md`;
- changing package version, CHANGELOG, UPM archive entries, public release
  assets, compiler behavior, geometry, materials, samples, Tests, Runtime,
  Schema, or dependencies;
- generated marketing art, ImageGen, Blender, stock meshes, screenshots with
  editor chrome, or reuse of untracked `Project~` scenes/results;
- repository social-preview publication, which is an external presentation
  action after proof assets are audited;
- issue #21 or future geometry milestones.

## Acceptance

- batch generation starts from a clean scene and invokes only maintained
  FoldCanvas sample/proof construction paths;
- result PNGs are fixed-size, have no editor chrome, and contain no local path,
  username, credential, or license data;
- cup proof reports one closed volume, zero open/non-manifold/orientation-
  conflict edges, and a nonzero volume;
- sphere proof reports eight panels, Euler characteristic two, zero open,
  non-manifold, and orientation-conflict edges, and outward winding;
- result geometry hashes are recomputed from compiled data and locked in the
  manifest rather than inferred from image pixels;
- every source and output hash is checked; manifest arrays and diagnostics use
  deterministic ordinal order;
- proof generation never mutates tracked package source or persists generated
  Mesh assets as canonical source;
- complete repository validation, Unity Edit Mode tests, batch proof
  regeneration, `git diff --check`, and exact package-archive identity pass;
- hosted exact-head checks and an exact-head maintainer audit are green before
  merge.

## Architecture boundary

M21 adds a release-excluded clean-host Unity adapter plus release-excluded
documentation. The disposable host reads the maintained 2D source and runs the
ordinary deterministic Runtime compiler. PNGs and the manifest are derived
evidence. They never become input to geometry compilation and never replace
FoldScript, panels, seams, or the canvas.

## Implementation status

Implementation is complete locally on `agent/m21-proof-gallery-evidence`. No
open PR or external CLAIM existed when work started. Issue #26's original
combined acceptance was split because the root `README.md` is explicitly
included by `Scripts/build_release_package.py`; changing it cannot preserve
current UPM archive bytes. M21 therefore builds proof evidence first, while
README integration is deferred to a separately versioned patch iteration.

Unity `6000.3.20f1` regenerated the evidence twice from disposable clean hosts,
and all seven files were byte-identical. Six PNGs were visually inspected. The
full local repository matrix and exact stable-archive identity passed. Hosted
exact-head checks passed on audited head
`334fb01cc273888d3dadfbf188431cd49c23eb7d`; PR #35 merged as
`67fb659ff53e13f21e464b8a4e837b72bdc60c50`. M21 is complete.
