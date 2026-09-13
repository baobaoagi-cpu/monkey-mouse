// Monkey Mouse, modified 2026-09-13; GPL-2.0. Refusal paths only; no native storage writes.
using System.Diagnostics;
namespace Tests.Config;
[TestFixture]
public class ReviewToolTests
{
    [TestCase("identity-init")][TestCase("identity-show")][TestCase("invite-export")]
    [TestCase("socket-smoke")]
    [TestCase("peers")][TestCase("pair")][TestCase("revoke")][TestCase("storage-self-test")]
    public async Task ProtectedStorageCommand_RefusesWithoutExplicitFlag(string command)
    {
        var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"MonkeyMouse.Tools")))root=root.Parent;
        if(root is null)throw new InvalidOperationException("Source tree not found");
        var config=new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
        var start=new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")??"dotnet")
        {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add(Path.Combine(root.FullName,"MonkeyMouse.Tools","bin",config,"net10.0","MonkeyMouse.dll"));
        start.ArgumentList.Add(command);
        using var process=Process.Start(start)!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.That(process.ExitCode,Is.EqualTo(2));
        Assert.That(await process.StandardError.ReadToEndAsync(),Does.Contain(command == "socket-smoke" ? "--allow-loopback" : "--allow-store-write"));
    }
}
