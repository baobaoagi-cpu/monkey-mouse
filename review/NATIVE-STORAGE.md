# Native protected-storage validation — pending

Modified 2026-09-13; GPL-2.0. Both adapters compile. Neither native protected-store write test has been executed in this review. Do not infer native compatibility from the in-memory TLS tests.

A concrete opt-in test is available after build:

```sh
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll storage-self-test --allow-store-write
```

This writes random test bytes, reads and compares them, updates them, reads again, then removes only its own unique test item. It does not create a device identity or peer approval. Do not execute it until the operator has approved this exact write action. No permission prompt suppression, entitlement bypass or plaintext fallback is provided. A signing/entitlement failure must be reported and resolved through supported signing, not by lowering OS security.

On macOS, the item uses service `MonkeyMouse.local-review.device-vault.v1.self-test.<random-guid>`, account `local-device`, the data-protection keychain, no synchronization and WhenUnlockedThisDeviceOnly. Cleanup targets only that service/account. An empty exclusive lock file remains under the platform's per-user local application-data directory in `MonkeyMouse/review/`.

On Windows, it uses current-user DPAPI with UI forbidden, writes only to the platform's per-user local application-data directory under `MonkeyMouse/review/self-test/<random-guid>/vault.dpapi`, then removes that encrypted test file. An empty directory and lock file remain. Cleanup never traverses or deletes other credential stores. A process termination during the test can leave its isolated test item; record the failure and inspect that exact ID before cleanup.

Success on one OS is not success on the other. Record OS/build, result, failures and the identity of the isolated test item locally, not in a public commit. No durable-state command has a plaintext backup. Future production work still needs recovery/rotation policy and signing/entitlement validation.
