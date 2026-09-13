// Modified 2026-09-13; GPL-2.0. Explicit roles and measured Hydra coordinates required.
using Hydra.Screen;

namespace Hydra.Config;

public sealed record DisplayMeasurement(string? ScreenId, int? X, int? Y, int? Width, int? Height, decimal? Scale);
public sealed record FourScreenMeasurements(DisplayMeasurement WindowsExternal, DisplayMeasurement WindowsLaptop,
    DisplayMeasurement MacBook, DisplayMeasurement BenQ, string? CoordinateSpace,
    int SourceStart = 0, int SourceEnd = 100, int DestStart = 0, int DestEnd = 100);

public static class FourScreenProfileBuilder
{
    public static HydraConfig Build(FourScreenMeasurements data)
    {
        if (data.CoordinateSpace != "HydraReported") throw new InvalidOperationException("Confirm coordinate space using Hydra's read-only display enumeration first");
        var all = new[] { data.WindowsExternal, data.WindowsLaptop, data.MacBook, data.BenQ };
        foreach (var d in all)
            if (string.IsNullOrWhiteSpace(d.ScreenId) || d.ScreenId.Contains("REPLACE",StringComparison.OrdinalIgnoreCase) ||
                d.X is null || d.Y is null || d.Width is null or <=0 || d.Height is null or <=0 || d.Scale is null or <=0)
                throw new InvalidOperationException("All four display IDs, bounds and scales must be measured");
        if (all.Select(d=>d.ScreenId).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=4)
            throw new InvalidOperationException("Display IDs must uniquely identify the four roles");
        RequireAdjacent(data.WindowsExternal,data.WindowsLaptop,"Windows external must be left of Windows laptop");
        RequireAdjacent(data.MacBook,data.BenQ,"MacBook must be left of BenQ");
        if(data.SourceStart<0||data.SourceEnd>100||data.SourceStart>=data.SourceEnd||data.DestStart<0||data.DestEnd>100||data.DestStart>=data.DestEnd)
            throw new InvalidOperationException("Edge ranges must be increasing percentages in 0..100");
        List<HostConfig> hosts=[new(){Name="ASUS",Neighbours=[new(){Name="Mac",Direction=Direction.Down,
            SourceScreen=data.WindowsExternal.ScreenId,DestScreen=data.BenQ.ScreenId,Mirror=true,
            SourceStart=data.SourceStart,SourceEnd=data.SourceEnd,DestStart=data.DestStart,DestEnd=data.DestEnd}]},new(){Name="Mac"}];
        HydraConfig.ExpandMirrors(hosts);
        return new HydraConfig{Mode=Mode.Master,ProfileName="four-screen-approved-session",Hosts=hosts,
            EnableFileTransfer=false,SyncScreensaver=false,ScreenLockPropagation=false,AllowRemoteLock=false,AllowRemoteWake=false};
    }
    private static void RequireAdjacent(DisplayMeasurement left,DisplayMeasurement right,string error)
    {
        if((long)left.X!.Value+left.Width!.Value!=right.X!.Value ||
            Math.Max((long)left.Y!.Value,right.Y!.Value)>=Math.Min((long)left.Y.Value+left.Height!.Value,(long)right.Y.Value+right.Height!.Value))
            throw new InvalidOperationException(error+" with touching edges and vertical overlap");
    }
}
