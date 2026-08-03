# AI integration contract

## Core rule

AI systems generate or revise **FoldCanvas source**, never final Mesh binaries.

```text
User intent / image / sketch
        ↓
external provider adapter proposes FoldScript + appearance
        ↓
bounded FoldScript importer
        ↓
deterministic compiler
        ↓
structured diagnostics
        ↓
external adapter proposes complete replacement FoldScript
        ↓
the same importer and compiler gates
```

The source remains reviewable, diffable, editable, and reproducible. A failed
compile is never replaced silently by unrelated triangles.

## M08 provider-neutral API

The Runtime assembly exposes synchronous data contracts only:

```csharp
public interface IFoldCanvasSourceProposer
{
    FoldCanvasSourceProposal Propose(
        FoldCanvasSourceProposalRequest request);
}

public interface IFoldCanvasSourceRepairer
{
    FoldCanvasRepairResponse Repair(FoldCanvasRepairRequest request);
}
```

No implementation, provider SDK, authentication flow, credential, HTTP client,
or background request is included. An external package may implement these
interfaces for any provider without gaining privileged access to the compiler.
The core package remains fully usable offline.

## Proposal constraints

A proposal returns complete FoldScript JSON. It must contain:

- exact schema version `0.1`;
- stable, unique IDs and explicit units;
- bounded finite values and collection counts;
- only documented panel/operation types and valid references;
- a safe project-relative appearance reference.

The format contains no executable code, arbitrary polymorphic type name,
external URL fetch, embedded credential, base64 geometry, or Mesh buffer.

## Repair request

`FoldCanvasRepairRequestBuilder.Create` combines the canonical document with an
ordinary `FoldCanvasCompileResult`. `ToCanonicalJson()` emits fixed-order data:

- `schemaVersion`, `compilerVersion`, and `assetId`;
- the complete canonical FoldScript in `source`;
- ordered diagnostics with code, severity, message, panel/operation/seam/
  boundary and localized geometry context;
- ordered structured numeric values and repair suggestions.

The request deliberately excludes Unity Mesh objects, vertex positions,
triangle buffers, texture pixels, provider metadata, accounts, and credentials.
Collections are defensive read-only copies and repeated builds of identical
source/diagnostics produce byte-identical payloads.

## Repair response and acceptance

A repair response is complete replacement FoldScript JSON. It is not a patch
language and cannot mutate compiler buffers. `FoldCanvasRepairCoordinator.Apply`
passes it through:

1. bounded JSON parse;
2. strict FoldScript semantic and reference validation;
3. explicit unit-aware native conversion;
4. the ordinary deterministic geometry compiler and M07 validation.

Malformed, unsafe, or still-invalid responses return stable diagnostics and no
successful Mesh. M08 does not automatically send requests or accept changes;
transport and human review belong to an external integration.

## Evaluation

AI-assisted source quality is judged by:

- import and compile success;
- manifold/closed topology when required;
- seam and winding evidence;
- UV correctness and preserved source artwork;
- editable parameters and stable identifiers;
- deterministic rebuild and canonical round-trip;
- source simplicity and number of repair iterations.

Rendered resemblance alone is not acceptance.

## Safety boundary

Treat all model output as untrusted text. The M08 reader limits total
characters, nesting depth, node count, string length, identifier length,
collection counts, and numeric ranges before native source construction.
Appearance paths must remain under approved `Assets/` or `Packages/` roots.
Runtime performs no file or network I/O; Editor import validates a detached
source before replacing an explicitly selected asset and records Undo on an
existing FoldCanvas destination.

See [the M08 runtime guide](foldscript-runtime.md),
[FoldScript fields](foldscript-field-reference.md), and
[diagnostics](diagnostics.md).
