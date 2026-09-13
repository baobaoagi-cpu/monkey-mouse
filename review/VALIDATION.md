# Monkey Mouse validation record

Prepared 2026-09-13. Development source only; no production readiness claim.

- macOS ARM64, .NET SDK 10.0.401: review tooling and engine compile with warnings treated as errors and macOS helper skipped.
- Bounded safety/regression suite: **661 passed, 0 failed, 0 skipped**. It includes in-memory certificate/TLS framing and pairing tests, text-only clipboard policies, four-screen geometry, legacy denial paths, storage-write refusal, and two-process lock contention and recovery after normal exit or process termination.
- Static review checks: **14 passed**. These cover disabled runtime and legacy network entry points, no installer attribute removal/resigning, no permissive signing requirement, and safe profile flags.
- TLS observed in the managed macOS tests: TLS 1.2 with the allowed ECDHE-ECDSA/AES-GCM suites. TLS 1.3 interoperability is not verified.
- Native Keychain and DPAPI adapters: **compiled only**. The explicit isolated-item self-test is prepared but has not been run. No persistent identity/peer trust was created by this validation.
- A read-only display-enumeration command ran successfully; actual personal display records are excluded from this repository. Physical four-screen movement, Windows platform execution and real clipboard exchange remain untested.

Tests create identities only in an in-memory test store. On macOS the .NET certificate implementation can allocate disposable temporary keychains and releases them on disposal; this is separate from the untested persistent Keychain adapter. Lease probes create only uniquely named scratch lock files and child test processes. No KVM host, system helper, relay listener, firewall rule or OS input permission is started/changed.

This is a selected safety suite, not the entire inherited Hydra/WebConfig test collection and not a full security audit. Old standalone server, manual integration and web configuration code is retained for source completeness, not approved for use. Public export excludes inherited automatic release workflows, personal inventories, logs, generated binaries/certificates and native state. Fixed words such as `test` in inherited test fixtures are synthetic test inputs, not actual credentials.

## Secure socket slice (2026-09-13)

- Mac ARM64: 672 selected tests pass, including 10 opt-in loopback TCP/TLS tests; 14 static checks and the simulated cp950 source-check regression pass. The original 661 cases remain included, plus one CLI loopback opt-in refusal test.
- Both win-x64 and osx-arm64 target restores pass in locked mode without rewriting any lockfile. All pre-existing dependency versions and contentHash values remain unchanged.
- New socket tests exercise synthetic key/mouse/text dispatch, existing slave fake input/clipboard handling, held-key release on revocation, reconnect, unapproved and impostor denial, receive capabilities, cancellation and non-loopback denial.
- Default review/test.sh does not open sockets. --with-loopback and socket-smoke --allow-loopback explicitly opt into localhost only. No persistent Keychain/DPAPI, OS input/clipboard, LAN session or physical four-screen validation has occurred.

- Review tooling cross-build for win-x64 on Mac succeeds with zero warnings/errors and produces a Windows x86-64 PE apphost. This is not execution on Windows. The standalone socket-smoke CLI reports PASS on Mac. Default no-socket selection: 662 tests pass, 0 fail/skip; opt-in selection: 672 pass.
