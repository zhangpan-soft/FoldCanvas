# Current task

Execute **M23: 1.0.1 patch release**.

Authoritative task file:
[`Codex/M23_PATCH_RELEASE.md`](Codex/M23_PATCH_RELEASE.md)

M22 is complete. PR #36 merged exact audited head
`8edd04bf6121162e92b70ec4f0468105ca0b6b13` as merge commit
`1ff4a1b9043304450884a8ab140407dd06ec1670`; issue #26 is closed. Post-merge
repository checks, full Unity workflow, and M13 robustness long run all passed.

M23 is the explicit release milestone promised by M22. It must bind exact
package `1.0.1` bytes to a patch-release contract, retain immutable `v1.0.0` as
rollback, prove two clean public consumers and a source-first `1.0.0` to
`1.0.1` upgrade, and publish only an annotated tag that peels to the audited
merge.

Implementation is active on `agent/m23-patch-release-qualification`.

The maintainer may research, plan, implement, audit, merge, tag, publish the
repository's GitHub release, and close this repository-only milestone
autonomously. Credentials, paid services, irreversible permission changes,
legal decisions, and external marketplace publication remain owner escalation
points.
