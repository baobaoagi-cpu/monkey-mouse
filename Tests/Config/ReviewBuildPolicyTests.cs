// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
using System.Reflection;
using Hydra.Config;
using Hydra.Platform.MacOs;
using Hydra.Update;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Setup;

namespace Tests.Config;

[TestFixture]
public class ReviewBuildPolicyTests
{
    [TestCase("--install")]
    [TestCase("--uninstall")]
    [TestCase("--service")]
    [TestCase("--session")]
    public void BackgroundCommands_RefusedBeforePlatformWork(string command) =>
        Assert.Throws<NotSupportedException>(() => ReviewBuildPolicy.EnsureManualSession([command]));

    [Test]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    public void MacInstall_DirectCallAlsoRefused()
    {
        if (!OperatingSystem.IsMacOS()) { Assert.Ignore("macOS API guard"); return; }
        Assert.Throws<NotSupportedException>(() => AgentCommands.Install());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task UpdateCycle_IsNoOpEvenWithLegacyOptIn(bool autoUpdate)
    {
        using var updater = new SelfUpdater(new HydraProfile(new HydraConfigFile { AutoUpdate = autoUpdate }, null), NullLogger<SelfUpdater>.Instance);
        var execute = typeof(SelfUpdater).GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance)!;
        // Invoke the cycle itself without starting a hosted service, timer, or application.
        await (Task)execute.Invoke(updater, [CancellationToken.None])!;
    }
}
