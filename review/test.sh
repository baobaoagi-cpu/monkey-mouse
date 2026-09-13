#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$PWD/.review-cache/dotnet-cli}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$PWD/.review-cache/nuget}"
socket_filter='&TestCategory!=Loopback'
case "${1:-}" in
  '') ;;
  --with-loopback) socket_filter='' ;;
  *) echo "Usage: bash review/test.sh [--with-loopback]" >&2; exit 2 ;;
esac
dotnet restore MonkeyMouse.Tools/MonkeyMouse.Tools.csproj -p:SkipMacShield=true --locked-mode
dotnet build MonkeyMouse.Tools/MonkeyMouse.Tools.csproj --no-restore -p:SkipMacShield=true
dotnet restore Tests/Tests.csproj -p:SkipMacShield=true --locked-mode
dotnet test Tests/Tests.csproj --no-restore -p:SkipMacShield=true --filter "(FullyQualifiedName~Tests.Security.|FullyQualifiedName~Tests.Relay.SafePolicyTests|FullyQualifiedName~Tests.FileTransfer.|FullyQualifiedName~Tests.Relay.ActivityTrackerTests|FullyQualifiedName~Tests.Relay.SlaveLockScreenTests|FullyQualifiedName~Tests.Relay.SlaveDormancyTests|FullyQualifiedName~Tests.Screen.|FullyQualifiedName~Tests.Relay.ClipboardSyncTests|FullyQualifiedName~Tests.Config.)${socket_filter}" --logger 'trx;LogFileName=safety-tests.trx' --results-directory "$PWD/.review-results"
python3 review/check_source.py
python3 review/test_source_encoding.py
