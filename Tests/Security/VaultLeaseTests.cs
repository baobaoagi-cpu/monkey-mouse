// Monkey Mouse, modified 2026-09-13; GPL-2.0. Scratch locks only; no Keychain/DPAPI/network.
using System.Diagnostics;
using Hydra.Security;
namespace Tests.Security;
[TestFixture]
public class VaultLeaseTests
{
    [TestCase(false)][TestCase(true)]
    public async Task CompetingProcess_IsRejected_ThenCanAcquireAfterExit(bool killOwner)
    {
        var directory=Path.Combine(Path.GetTempPath(),"monkey-mouse-lease-tests",Guid.NewGuid().ToString("N"));
        var lockPath=Path.Combine(directory,"vault.lock");
        Process? owner=null;
        try
        {
            owner=Start(lockPath);
            Assert.That(await owner.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)),Is.EqualTo("LEASED"));
            Assert.Throws<IOException>(()=>VaultLease.Acquire(lockPath));
            using(var contender=Start(lockPath))
            {
                await contender.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.That(contender.ExitCode,Is.EqualTo(3));
                Assert.That(await contender.StandardOutput.ReadLineAsync(),Is.EqualTo("BUSY"));
            }
            if(killOwner)owner.Kill();else await owner.StandardInput.WriteLineAsync();
            await owner.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(File.Exists(lockPath),Is.True,"Lock inode must never be unlinked by an owner");
            using var replacement=VaultLease.Acquire(lockPath);
            Assert.Throws<IOException>(()=>VaultLease.Acquire(lockPath));
        }
        finally
        {
            if(owner is not null){if(!owner.HasExited){owner.Kill();await owner.WaitForExitAsync();}owner.Dispose();}
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
    }
    private static Process Start(string path)
    {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"LeaseProbe")))root=root.Parent;
        if(root is null)throw new InvalidOperationException("Source tree not found");
        var config=new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var probe=Path.Combine(root.FullName,"LeaseProbe","bin",config,"net10.0","LeaseProbe.dll");
        var start=new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")??"dotnet")
        {UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add(probe);start.ArgumentList.Add(path);
        return Process.Start(start)??throw new InvalidOperationException("Probe failed to start");
    }
}
