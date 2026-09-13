// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace Hydra.Platform.MacOs;

[SupportedOSPlatform("macos")]
internal static partial class AgentCommands
{
    private const string Label = "com.cathedral.hydra";
    private const string PlistFileName = "com.cathedral.hydra.plist";

    [LibraryImport("libc")]
    private static partial uint getuid();

    private static string DomainTarget() => $"gui/{getuid()}";

    internal static void Install() => throw new NotSupportedException(
        "Offline review build: installation is disabled. A verified Developer ID signed and notarized release is required.");

    internal static void Uninstall()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var plistPath = Path.Combine(home, "Library", "LaunchAgents", PlistFileName);

        if (!File.Exists(plistPath))
        {
            Console.WriteLine("Hydra agent is not installed.");
            return;
        }

        RunLaunchctl($"bootout {DomainTarget()}/{Label}", tolerateFailure: true);
        File.Delete(plistPath);
        Console.WriteLine("Hydra agent removed.");
    }

    private static void RunLaunchctl(string args, bool tolerateFailure = false)
    {
        using var proc = Process.Start(new ProcessStartInfo("/bin/launchctl", args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("failed to start launchctl");

        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0 && !tolerateFailure)
            throw new InvalidOperationException($"launchctl {args} failed (exit {proc.ExitCode}): {output}{error}");
    }

    private static string GeneratePlist(string exePath, string workingDir, string logDir)
    {
        var exe = SecurityElement.Escape(exePath);
        var wd = SecurityElement.Escape(workingDir);
        var stdout = SecurityElement.Escape(Path.Combine(logDir, "hydra.stdout.log"));
        var stderr = SecurityElement.Escape(Path.Combine(logDir, "hydra.stderr.log"));

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>Label</key>
                <string>{Label}</string>
                <key>ProgramArguments</key>
                <array>
                    <string>{exe}</string>
                </array>
                <key>RunAtLoad</key>
                <true/>
                <key>KeepAlive</key>
                <true/>
                <key>StandardOutPath</key>
                <string>{stdout}</string>
                <key>StandardErrorPath</key>
                <string>{stderr}</string>
                <key>WorkingDirectory</key>
                <string>{wd}</string>
                <key>ThrottleInterval</key>
                <integer>5</integer>
            </dict>
            </plist>
            """;
    }
}
