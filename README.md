# Monkey Mouse

**Development review only. Input-controller startup is blocked. Four physical screens have not been tested. This cannot yet replace an installed KVM application.**

Monkey Mouse is a GPL-2.0 derivative of [PacAnimal/hydra](https://github.com/PacAnimal/hydra), based on commit `01b6dd8557c8d694ac15d03357bf6c9aef213733`. Original copyright and license notices are retained. See [LICENSE](LICENSE) and [MODIFICATIONS.md](MODIFICATIONS.md). No warranty; no upstream endorsement. `monkey-mouse/` is the intended development directory when adding this source to an existing repository.

The intended layout is Windows external display on the upper left, Windows laptop on the upper right, MacBook on the lower left and a BenQ display on the lower right. The Windows external bottom edge connects to the BenQ top edge in both directions. Movement between the Mac displays stays local. Real display IDs, geometry and scaling must be collected on both computers; this repository contains only templates and synthetic test fixtures.

Implemented for offline review:

- Per-device ECDSA identities, full-fingerprint confirmation on each device, capability grants, explicit revocation, and mutual TLS over caller-supplied streams.
- A terminal pairing workflow with separate input-sender/input-receiver roles and bidirectional plain-text clipboard permission. Both devices must approve independently.
- Lifetime exclusive vault ownership to prevent a second cooperating process overwriting trust with stale state. Sessions close before ownership is released.
- Keychain and current-user DPAPI adapters, plus an explicitly gated isolated-item self-test. Native adapters are compiled but their create/read/update/delete behavior is **not yet tested**.
- Four-screen route validation; file transfer, remote lock/wake, screensaver sync, legacy password relay and automatic updating are disabled.

Still required: native-store validation on macOS and Windows, production pairing UI and network hosting, signed/notarized distribution, physical four-screen movement and clipboard tests, and a full dependency/redistribution audit. No usable network session is exposed. The inherited engine namespaces and protocol/config types remain named Hydra for traceability; that is not the product name. Inherited WebConfig, Styx standalone server and publishing assets are not an approved product workflow.

## Build and verify

Requires .NET SDK 10.0.401. From this directory:

```sh
bash review/test.sh
```

The first restore uses NuGet. Helper compilation is skipped by default and explicitly by the script. This command builds the review tools, runs the bounded regression suite and static checks. It does not install or start an input controller, network listener or native protected-store test. Managed certificate tests on macOS may use .NET-owned disposable temporary keychains, never persistent login/system trust entries.

Read-only review tooling after the build:

```sh
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll --help
dotnet MonkeyMouse.Tools/bin/Debug/net10.0/MonkeyMouse.dll displays --host Mac
# On Windows use --host ASUS
```

Keep display inventories, pairing invitations, fingerprints and all native state out of public commits. See [review/PAIRING.md](review/PAIRING.md), [review/NATIVE-STORAGE.md](review/NATIVE-STORAGE.md), and [review/VALIDATION.md](review/VALIDATION.md).
