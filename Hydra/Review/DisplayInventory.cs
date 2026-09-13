// Modified 2026-09-13 for Monkey Mouse; GPL-2.0. Only display enumeration, no input hooks/permissions.
using Hydra.Platform;
using Hydra.Platform.MacOs;
using Hydra.Platform.Windows;
using Hydra.Screen;
namespace Hydra.Review;

public sealed record DisplayInventoryItem(string EngineScreenId,string? PlatformId,string? OutputName,string? DisplayName,
    int X,int Y,int Width,int Height,int AdvertisedX,int AdvertisedY,decimal EngineDefaultMouseScale);
public sealed record DisplayInventorySnapshot(string Product,string Host,string CoordinateSpace,DateTimeOffset CapturedAt,List<DisplayInventoryItem> Displays);
public static class DisplayInventory
{
    public static DisplayInventorySnapshot Read(string host)
    {
        if(host is not ("Mac" or "ASUS"))throw new ArgumentException("Host must be Mac or ASUS for the four-screen profile");
        List<DetectedScreen> displays;
        if(OperatingSystem.IsMacOS())displays=MacDisplayHelper.GetAllScreens();
        else if(OperatingSystem.IsWindows())displays=WindowsDisplayHelper.GetAllScreens();
        else throw new PlatformNotSupportedException("Only macOS and Windows are supported");
        return FromDetected(host,displays);
    }
    public static DisplayInventorySnapshot FromDetected(string host,List<DetectedScreen> displays)
    {
        if(displays.Count==0)throw new InvalidOperationException("No displays detected");
        var minX=displays.Min(d=>d.X);var minY=displays.Min(d=>d.Y);
        return new("Monkey Mouse",host,"HydraReported",DateTimeOffset.UtcNow,
            displays.Select((d,i)=>new DisplayInventoryItem(ScreenNaming.BuildScreenName(host,i,displays.Count),d.PlatformId,d.OutputName,d.DisplayName,
                d.X,d.Y,d.Width,d.Height,d.X-minX,d.Y-minY,1.0m)).ToList());
    }
}
