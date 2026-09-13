#!/usr/bin/env python3
"""Integration regression: restore the two actual target graphs without rewriting any lockfile.
Uses dotnet from PATH, or --dotnet with an existing SDK executable. May fetch locked NuGet assets.
This is target restore, not Windows execution or OS impersonation. No controller or sockets start.
"""
# Monkey Mouse, modified 2026-09-13; GPL-2.0.
import argparse
import os
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("--dotnet", default="dotnet")
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
locks = {p: p.read_bytes() for p in root.glob("*/packages.lock.json")}
env = os.environ.copy()
env.update(DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false",
           DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
# Respect caller-provided isolated caches; otherwise use repository-local ignored directories.
env.setdefault("DOTNET_CLI_HOME", str(root / ".review-cache/dotnet-cli"))
env.setdefault("NUGET_PACKAGES", str(root / ".review-cache/nuget"))
try:
    for rid in ("win-x64", "osx-arm64"):
        subprocess.run([args.dotnet, "restore", "Hydra/Hydra.csproj", "--locked-mode", "-p:RuntimeIdentifier=" + rid,
                        "-p:SkipMacShield=true"], cwd=root, env=env, check=True)
    # Leave the assets in their ordinary portable review configuration.
    subprocess.run([args.dotnet, "restore", "Tests/Tests.csproj", "--locked-mode",
                    "-p:SkipMacShield=true"], cwd=root, env=env, check=True)
finally:
    assert all(p.read_bytes() == data for p, data in locks.items()), "Restore rewrote a lockfile"
print("PASS: win-x64 + osx-arm64 locked restore; all lockfiles unchanged")
