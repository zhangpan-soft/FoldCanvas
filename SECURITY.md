# Security policy

FoldCanvas is a local Unity Editor package and currently performs no network requests.

## Supported versions

The supported stable package is `1.0.0` on Unity `6000.3.20f1`. Immutable
`v1.0.0-rc.2` is the first rollback target; `v1.0.0-rc.1` and preview
`0.1.0-preview.21` are historical deeper fallbacks and receive fixes only when
required to provide a safe downgrade path.

## Report a vulnerability

Do not post a suspected arbitrary-code-execution, path-traversal, unsafe deserialization, or malicious asset-import issue publicly before maintainers can assess it. Use the repository's GitHub private vulnerability-reporting page.

Do not include Unity license files, passwords, OAuth tokens, private source
artwork, or other credentials in a report. The maintainer will ask for a
sanitized minimal FoldScript if source is needed to reproduce the issue.

## Security boundaries

- FoldScript importers must treat external files as untrusted input.
- Paths emitted by imported documents must remain inside explicitly selected project directories.
- Importers must not execute embedded code or shell commands.
- AI provider adapters, when introduced, must remain optional and outside the deterministic geometry core.
