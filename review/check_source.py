#!/usr/bin/env python3
"""Static regression checks only: never invokes installers or platform helpers."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
agent = (root / 'Hydra/Platform/MacOs/AgentCommands.cs').read_text()
updater = (root / 'Hydra/Update/SelfUpdater.cs').read_text()
program = (root / 'Hydra/Program.cs').read_text()
project = ET.parse(root / 'Hydra/Hydra.csproj').getroot()
cycle = updater.split('protected override Task Execute(')[1].split('private async Task CheckAndUpdate(')[0]
commands = [e.attrib['Command'] for e in project.iter('Exec')]
fragment = json.loads((root / 'review/safe-profile.fragment.json').read_text())
legacy = (root / 'Hydra/Relay/RelayConnection.cs').read_text()
secure = (root / 'Hydra/Security/SecureMessagePolicy.cs').read_text()
checks = {
    'legacy_network_loop_removed': 'HubConnectionBuilder' not in legacy and 'new RelayEncryption' not in legacy,
    'legacy_data_rejected': 'Receive(string sourceHost, string sourceIp, byte[] payload) => Task.CompletedTask' in legacy,
    'runtime_disabled_before_platform_init': program.index('if (!ReviewBuildPolicy.RuntimeEnabled)') < program.index('Console.OutputEncoding'),
    'secure_unknown_message_deny_default': '_ => false' in secure,

    'installer_has_no_attribute_deletion': all(t not in agent for t in ('xattr', 'RemoveQuarantine', 'com.apple.quarantine', 'com.apple.provenance')),
    'installer_has_no_resigning': 'Codesign' not in agent and 'codesign' not in agent,
    'installer_direct_call_refused': 'Install() => throw new NotSupportedException' in agent,
    'updater_has_no_resigning': 'Codesign' not in updater and 'codesign' not in updater,
    'update_cycle_no_side_effects': all(t not in cycle for t in ('Cleanup(', 'CheckAndUpdate(', 'Http.', 'Process.', 'File.', 'Restart(')),
    'no_permissive_build_requirement': all('--requirements' not in c for c in commands),
    'background_guard_precedes_platform_init': program.index('ReviewBuildPolicy.EnsureManualSession(args)') < program.index('Console.OutputEncoding'),
    'no_installer_dispatch': 'ServiceCommands.Install()' not in program and 'AgentCommands.Install()' not in program,
    'fragment_safe_flags': all(fragment['profiles'][0][k] is False for k in ('enableFileTransfer', 'syncScreensaver', 'screenLockPropagation', 'allowRemoteLock', 'allowRemoteWake')),
    'fragment_no_secrets_or_network': not any(k in fragment['profiles'][0] for k in ('networkConfig', 'embeddedStyx', 'embeddedStyxServer')),
}
print(json.dumps(checks, indent=2))
assert all(checks.values()), 'Static review regression failed'
