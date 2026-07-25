# M08: FoldScript import/export and AI repair contract

## Visible proof

A cup source exports to canonical JSON, reimports without semantic loss, rejects malformed or hostile input, compiles, emits a structured diagnostic payload, and accepts a corrected second version.

## Import/export

- implement schema version `0.1`
- canonical property and array ordering for exporter
- stable float formatting with invariant culture
- preserve IDs and operation order
- resolve appearance paths only inside approved Unity project locations
- no embedded executable code
- no arbitrary external file access
- size and count limits before allocation

## Unity representation

Define explicit converters between FoldScript DTOs and Unity `FoldCanvasAsset`. Do not serialize Unity object internals directly as the public format.

## AI boundary

Create provider-neutral interfaces only. No OpenAI, local-model, or other SDK dependency in the core package.

Suggested contracts:

```text
IFoldCanvasSourceProposer
IFoldCanvasSourceRepairer
FoldCanvasRepairRequest
FoldCanvasRepairResponse
```

These interfaces may live in a separate optional assembly/package if that keeps the core cleaner.

## Repair payload

Include:

- schema and compiler version
- source asset ID
- stable diagnostics
- compact numeric context
- permitted repair suggestions
- no binary mesh

## Tests

- canonical round-trip
- unknown schema major rejected
- unknown operation rejected
- NaN/infinity rejected
- path traversal rejected
- excessive array count rejected
- duplicate IDs rejected
- valid repair changes source and clears targeted diagnostic
