#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_HOME="$PWD/.review-cache/dotnet-cli"
export NUGET_PACKAGES="$PWD/.review-cache/nuget"
dotnet restore MonkeyMouse.Tools/MonkeyMouse.Tools.csproj -p:SkipMacShield=true --locked-mode
dotnet build MonkeyMouse.Tools/MonkeyMouse.Tools.csproj --no-restore -p:SkipMacShield=true
dotnet restore Tests/Tests.csproj -p:SkipMacShield=true --locked-mode
dotnet test Tests/Tests.csproj --no-restore -p:SkipMacShield=true --filter 'FullyQualifiedName~Tests.Security.|FullyQualifiedName~Tests.Relay.SafePolicyTests|FullyQualifiedName~Tests.FileTransfer.|FullyQualifiedName~Tests.Relay.ActivityTrackerTests|FullyQualifiedName~Tests.Relay.SlaveLockScreenTests|FullyQualifiedName~Tests.Relay.SlaveDormancyTests|FullyQualifiedName~Tests.Screen.|FullyQualifiedName~Tests.Relay.ClipboardSyncTests|FullyQualifiedName~Tests.Config.' --logger 'trx;LogFileName=safety-tests.trx' --results-directory "$PWD/.review-results"
python3 review/check_source.py
