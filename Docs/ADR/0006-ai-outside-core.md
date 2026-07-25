# ADR 0006: AI providers remain outside the core package

- Status: Accepted
- Date: 2026-07-25

## Decision

`com.foldcanvas.core` has no network, account, SDK, or model-provider dependency. AI integrations are separate optional packages that emit and repair FoldCanvas source.

## Consequences

- The project remains useful offline.
- Contributors can build multiple provider adapters.
- AI output must pass the same schema and compiler validation as human-authored source.
