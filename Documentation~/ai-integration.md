# AI integration contract

## Core rule

AI systems generate or revise **FoldCanvas source**, not final mesh binaries.

```text
User intent / image / sketch
        ↓
AI source proposal
├── appearance canvas
├── panel segmentation
├── boundaries
├── seams
└── FoldScript operations
        ↓
Deterministic compiler
        ↓
Structured diagnostics
        ↓
AI or human source repair
```

## Provider isolation

Provider integrations live outside `FoldCanvas.Runtime`. The core package must compile and be useful with no account, key, network connection, or model download.

A future provider package might be named:

```text
com.foldcanvas.ai.openai
com.foldcanvas.ai.local
com.foldcanvas.ai.generic
```

No provider is privileged by the source format.

## AI output constraints

An AI adapter should produce:

- schema-valid JSON
- stable IDs
- explicit units
- bounded numeric values
- no embedded executable code
- no arbitrary file paths
- no mesh or base64 geometry blobs in the MVP contract

## Feedback payload

Compiler diagnostics should be serializable into a compact repair request:

```json
{
  "assetId": "gpt-cup",
  "compilerVersion": "0.3.0",
  "errors": [
    {
      "code": "FC2104",
      "seam": "attach-bottom",
      "relativeDifference": 0.032,
      "suggestions": [
        "increase wall physical width",
        "set radiusMode to fitTargetBoundary"
      ]
    }
  ]
}
```

## Evaluation

AI success is not judged only by rendered resemblance. It is judged by:

- compile success
- manifold topology when required
- seam error
- UV correctness
- parameter editability
- deterministic rebuild
- source simplicity
- diagnostic repair count

## Safety

Treat model output as untrusted data. Validate schema, finite numbers, array limits, path references, and operation counts before compilation.
