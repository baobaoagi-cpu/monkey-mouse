// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
namespace Hydra.Config;

internal static class ReviewBuildPolicy
{
    internal static bool RuntimeEnabled => false;
    internal static void EnsureManualSession(string[] args)
    {
        if (args.Any(a => a is "--install" or "--uninstall" or "--service" or "--session"))
            throw new NotSupportedException("Offline review build: background installation, removal and service modes are disabled.");
    }
}
