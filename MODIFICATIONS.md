# Modification notice — 2026-09-13

This independent offline safety review fork modifies PacAnimal/hydra at commit `01b6dd8557c8d694ac15d03357bf6c9aef213733`. Changes were prepared with Codex for this local review task. It is not an upstream release or an endorsement by the upstream author.

The original root GPL-2.0 LICENSE and upstream copyright notices are preserved. The modified work is supplied under GPL-2.0, without warranty. New safety policy and test code is supplied under GPL-2.0. Package dependency license declarations remain separate and require a complete redistribution review before publishing binaries.

Changes: deny-by-default file/system-control policy, file service entry guards, local consent for sync/lock/wake, blocked background commands and self-update, removed installation quarantine deletion and permissive runtime signing, development helper signature correction, isolated managed test switch, explicit legacy opt-in tests, new regression tests, locked dependencies and review documentation.

The inherited Hydra.csproj Copyright text mentions Apache 2.0 while the root license is GPL-2.0. That upstream metadata inconsistency is preserved and documented rather than treated as authority to relicense the code. This patch follows the root GPL-2.0. The NuGet Cathedral dependency declares WTFPL; the dependency graph is locked but has not had a complete source audit.

Modified or added files at preparation time:

- `Common/packages.lock.json`
- `Hydra/Config/HydraConfig.cs`
- `Hydra/Config/HydraConfigFile.cs`
- `Hydra/Config/HydraProfile.cs`
- `Hydra/Config/ReviewBuildPolicy.cs`
- `Hydra/FileTransfer/FileTransferService.cs`
- `Hydra/Hydra.csproj`
- `Hydra/Platform/MacOs/AgentCommands.cs`
- `Hydra/Program.cs`
- `Hydra/Relay/ActivityTracker.cs`
- `Hydra/Relay/MessagePolicy.cs`
- `Hydra/Relay/RelayConnection.cs`
- `Hydra/Relay/SlaveRelayConnection.cs`
- `Hydra/Screen/InputRouter.cs`
- `Hydra/Update/SelfUpdater.cs`
- `Hydra/packages.lock.json`
- `README.md`
- `Styx/packages.lock.json`
- `Tests/Config/HydraConfigTests.cs`
- `Tests/Config/ReviewBuildPolicyTests.cs`
- `Tests/FileTransfer/DisabledFileTransferTests.cs`
- `Tests/FileTransfer/FileTransferServiceTests.cs`
- `Tests/Relay/ActivityTrackerTests.cs`
- `Tests/Relay/SafePolicyTests.cs`
- `Tests/Relay/SlaveDormancyTests.cs`
- `Tests/Relay/SlaveLockScreenTests.cs`
- `Tests/Screen/ScreenLockTests.cs`
- `Tests/Setup/FakeScreenSaverSync.cs`
- `Tests/Setup/TestableSlaveRelay.cs`
- `Tests/packages.lock.json`
- `docs/CONFIGURATION.md`
- `global.json`
- `review/README.md`
- `review/check_source.py`
- `review/safe-profile.fragment.json`
- `review/test.sh`

This notice itself was added on the same date.

## Phase 2 — modified 2026-09-13

Added DeviceVault, platform protected-blob adapters, approved mutual TLS sessions, authorized relay adapter, strict text clipboard filtering, disabled legacy network dispatch and executable startup, four-screen role/geometry builder, template/diagram/Windows read-only collector, and security/geometry regressions. Modified old image tests to assert the new rejection policy. Native secure-store integration, production UI/network hosting and hardware verification remain unfinished. All new source is supplied under GPL-2.0.

Phase 2 cumulative file inventory:

- `Common/packages.lock.json`
- `Hydra/Config/FourScreenProfileBuilder.cs`
- `Hydra/Config/HydraConfig.cs`
- `Hydra/Config/HydraConfigFile.cs`
- `Hydra/Config/HydraProfile.cs`
- `Hydra/Config/ReviewBuildPolicy.cs`
- `Hydra/FileTransfer/FileTransferService.cs`
- `Hydra/Hydra.csproj`
- `Hydra/Platform/ClipboardUtils.cs`
- `Hydra/Platform/MacOs/AgentCommands.cs`
- `Hydra/Program.cs`
- `Hydra/Relay/ActivityTracker.cs`
- `Hydra/Relay/MessagePolicy.cs`
- `Hydra/Relay/RelayConnection.cs`
- `Hydra/Relay/SlaveRelayConnection.cs`
- `Hydra/Screen/InputRouter.cs`
- `Hydra/Security/DeviceVault.cs`
- `Hydra/Security/PlatformBlobStores.cs`
- `Hydra/Security/SecureMessagePolicy.cs`
- `Hydra/Security/SecurePeerSession.cs`
- `Hydra/Security/SecureRelayAdapter.cs`
- `Hydra/Update/SelfUpdater.cs`
- `Hydra/packages.lock.json`
- `MODIFICATIONS.md`
- `README.md`
- `Styx/packages.lock.json`
- `Tests/Config/HydraConfigTests.cs`
- `Tests/Config/ReviewBuildPolicyTests.cs`
- `Tests/FileTransfer/DisabledFileTransferTests.cs`
- `Tests/FileTransfer/FileTransferServiceTests.cs`
- `Tests/Relay/ActivityTrackerTests.cs`
- `Tests/Relay/ClipboardSyncTests.cs`
- `Tests/Relay/SafePolicyTests.cs`
- `Tests/Relay/SlaveDormancyTests.cs`
- `Tests/Relay/SlaveLockScreenTests.cs`
- `Tests/Screen/FourScreenBuilderTests.cs`
- `Tests/Screen/FourScreenRouteTests.cs`
- `Tests/Screen/ScreenLockTests.cs`
- `Tests/Security/SecureSessionTests.cs`
- `Tests/Security/TextOnlyTests.cs`
- `Tests/Setup/FakeScreenSaverSync.cs`
- `Tests/Setup/TestableSlaveRelay.cs`
- `Tests/packages.lock.json`
- `docs/CONFIGURATION.md`
- `global.json`
- `review/README.md`
- `review/check_source.py`
- `review/four-screen/WINDOWS-CODEX-READONLY.txt`
- `review/four-screen/collect-windows-readonly.ps1`
- `review/four-screen/measurements.template.json`
- `review/four-screen/route.svg`
- `review/safe-profile.fragment.json`
- `review/test.sh`

## Monkey Mouse development phase — modified 2026-09-13

Public product branding is Monkey Mouse. Added a separate non-controller review CLI, interactive full-fingerprint pairing/revocation, read-only engine display inventory, explicitly gated isolated native-store self-test, and lifetime exclusive vault leases. Disposal invalidates trust and closes session observers before releasing ownership. Added terminal-flow and cross-process lock regressions. Default builds now skip the macOS helper. The engine assembly metadata is branded Monkey Mouse while original namespaces, copyrights and license provenance remain intact. Added public-safe review/build/storage documentation and ignore rules.

Native Keychain/DPAPI writes, production UI/network hosting and physical four-screen behavior remain unverified. The public source export omits inherited release/publishing automation so a new repository cannot accidentally publish upstream-branded binaries. GPL notices and complete source for all retained projects are included. No local inventory, native state, keys, certificates, logs or build output is part of the public source.

## Secure socket slice — modified 2026-09-13

Added loopback-only TCP host/client controllers that authenticate an approved DeviceVault peer before attaching SecureRelayAdapter to RelayConnection; serializes relay attachment and releases sessions on cancellation/revocation. Added an explicit socket-smoke CLI with memory-only pairing and synthetic sinks, opt-in loopback tests covering the existing slave input/clipboard/disconnect handling, and reconnection/denial tests. The production input-controller startup gate remains closed.

Fixed the complete .NET lock graph for Windows x64 and macOS ARM64 without upgrading existing packages, explicit UTF-8 source reads, a cp950 regression and a real dual-target locked-restore regression. Fixed test source-root discovery for worktrees/source archives. Default review tests exclude sockets; --with-loopback opts in. No persistent native storage or real LAN/input control is exercised by this change.
