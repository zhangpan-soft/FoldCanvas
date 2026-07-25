# Security policy

FoldCanvas is a local Unity Editor package and currently performs no network requests.

## Supported versions

Only the latest preview release is supported during pre-alpha development.

## Report a vulnerability

Do not post a suspected arbitrary-code-execution, path-traversal, unsafe deserialization, or malicious asset-import issue publicly before maintainers can assess it. Use GitHub's private vulnerability reporting when the repository is published.

## Security boundaries

- FoldScript importers must treat external files as untrusted input.
- Paths emitted by imported documents must remain inside explicitly selected project directories.
- Importers must not execute embedded code or shell commands.
- AI provider adapters, when introduced, must remain optional and outside the deterministic geometry core.
