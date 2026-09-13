// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
using Hydra.Platform;

namespace Tests.Setup;

public sealed class FakeScreenSaverSync : IScreenSaverSync
{
    public bool LockScreenCalled;
    public int ActivateCount;
    public int DeactivateCount;
    public bool ResetIdleTimerCalled;
    public int WakeDisplayCount;

    public event Action? ScreensaverActivated { add { } remove { } }
    public event Action? ScreensaverDeactivated { add { } remove { } }
    public event Action? ScreenLocked { add { } remove { } }
    public event Action? ScreenUnlocked { add { } remove { } }

    public void Activate() => ActivateCount++;
    public void Deactivate() => DeactivateCount++;
    public void LockScreen() => LockScreenCalled = true;
    public void ResetIdleTimer() => ResetIdleTimerCalled = true;
    public void WakeDisplay() => WakeDisplayCount++;
}
