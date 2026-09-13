# Monkey Mouse terminal pairing review

Modified 2026-09-13; GPL-2.0. This is a local preparation workflow, not a working input-sharing session. It does not add macOS Accessibility/Input Monitoring permissions, change a firewall or launch helpers. Native storage validation remains pending.

All commands below that access protected state require `--allow-store-write`; the flag is an explicit operator action. Even public inspection obtains a lock file. No such command is part of the automated regression script. Review native-storage instructions before executing one. Use the built `MonkeyMouse.dll`, not the inherited Hydra executable, whose startup is blocked.

After native-store testing and explicit approval to create real device identities:

1. Run `identity-init --allow-store-write` independently on each device. It refuses replacement of an existing identity. Never copy one device's identity to another.
2. Run `identity-show --allow-store-write` on both local screens. `invite-export --allow-store-write` emits only the public invitation; transfer that public JSON using your chosen existing method. Do not publish it in the repository.
3. On ASUS run `pair --invite <mac-public.json> --alias Mac --role send-input --allow-store-write`. Type the full SHA-256 fingerprint from the Mac's local display, then `PAIR`.
4. On Mac run `pair --invite <asus-public.json> --alias ASUS --role receive-input --allow-store-write`. Type the full fingerprint from the ASUS display, then `PAIR`.
5. `peers --allow-store-write` lists local approvals. One-sided approval never grants access on the other device. Pairing creates no network connection and no usable control session in this build.
6. To remove approval: `revoke --peer <full-sha256> --allow-store-write`, then `REVOKE`. A runtime process holding the vault must first release ownership; this review has no active controller. A future application needs an authenticated local control path for live CLI revocation.

The confirmation expires after two minutes and is one-use. Cancellation does not persist trust. Full fingerprint mismatch, missing state, failed protected storage, and conflicting writer ownership fail closed. Do not paste a login password into a terminal prompt or chat; pairing never asks for one. Changing a role or replacing a peer identity requires explicit revoke and fresh bilateral approval.

Two peers can hold separate local identities. Two processes cannot simultaneously open the same local vault. Persistent empty lock files are intentional: deleting a live lock inode would allow a second owner. The lock coordinates cooperating Monkey Mouse processes; it is not a defense against malicious code already executing as the same OS user.
