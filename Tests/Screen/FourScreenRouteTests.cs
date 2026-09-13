// Modified 2026-09-13; GPL-2.0. Synthetic geometry only; these are NOT measured display values.
using Hydra.Config;
using Hydra.Screen;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Screen;

[TestFixture]
public class FourScreenRouteTests
{
    private static readonly ScreenRect WinBig = new("win-external", "ASUS", -2560, 0, 2560, 1440, true);
    private static readonly ScreenRect WinLaptop = new("win-laptop", "ASUS", 0, 0, 1920, 1080, true);
    private static readonly ScreenRect MacBook = new("macbook", "Mac", 0, 0, 1512, 982, false);
    private static readonly ScreenRect BenQ = new("benq", "Mac", 1512, 0, 1920, 1080, false);
    private static (ScreenLayout Layout,List<HostConfig> Hosts) Build(int start=0,int end=100,int destStart=0,int destEnd=100)
    {
        List<HostConfig> hosts = [new() { Name="ASUS", Neighbours=[new() {Name="Mac",Direction=Direction.Down,
            SourceScreen=WinBig.Name,DestScreen=BenQ.Name,SourceStart=start,SourceEnd=end,DestStart=destStart,DestEnd=destEnd,Mirror=true}] },new(){Name="Mac"}];
        HydraConfig.ExpandMirrors(hosts);
        return(new ScreenLayout([WinBig,WinLaptop,MacBook,BenQ],hosts,0,[],NullLogger.Instance),hosts);
    }
    [TestCase(128)] [TestCase(640)] [TestCase(1280)] [TestCase(2304)]
    public void WindowsBigBottom_ToBenQTop_AndBack(int sourceX)
    {
        var (layout,hosts)=Build();
        var down=layout.DetectEdgeExit(WinBig,sourceX,WinBig.Height-1);
        Assert.That(down,Is.Not.Null);
        using(Assert.EnterMultipleScope())
        { Assert.That(down!.Destination,Is.SameAs(BenQ)); Assert.That(down.EntryY,Is.EqualTo(2));
          Assert.That(hosts.Single(h=>h.Name=="Mac").Neighbours.Single().DestScreen,Is.EqualTo(WinBig.Name)); }
        var up=layout.DetectEdgeExit(BenQ,down!.EntryX,0);
        Assert.That(up,Is.Not.Null);
        using(Assert.EnterMultipleScope())
        { Assert.That(up!.Destination,Is.SameAs(WinBig));Assert.That(up.EntryY,Is.EqualTo(WinBig.Height-3));Assert.That(up.EntryX,Is.EqualTo(sourceX).Within(2)); }
    }
    [Test]
    public void WrongScreensAndLocalHorizontalEdges_NeverCrossMachines()
    {
        var(layout,_)=Build();
        using(Assert.EnterMultipleScope())
        { Assert.That(layout.DetectEdgeExit(WinLaptop,100,WinLaptop.Height-1),Is.Null);
          Assert.That(layout.DetectEdgeExit(MacBook,100,0),Is.Null);
          Assert.That(layout.DetectEdgeExit(BenQ,0,500),Is.Null);
          Assert.That(layout.DetectEdgeExit(MacBook,MacBook.Width-1,500),Is.Null);
          Assert.That(layout.DetectEdgeExit(WinBig,WinBig.Width-1,500),Is.Null); }
    }
    [Test]
    public void PartialEdges_OnlyChosenSegments_AndMirrorSwapsRanges()
    {
        var(layout,hosts)=Build(20,80,10,90);
        Assert.That(layout.DetectEdgeExit(WinBig,100,WinBig.Height-1),Is.Null);
        Assert.That(layout.DetectEdgeExit(WinBig,2450,WinBig.Height-1),Is.Null);
        var down=layout.DetectEdgeExit(WinBig,1280,WinBig.Height-1)!;
        Assert.That(down.Destination,Is.SameAs(BenQ));Assert.That(down.EntryX,Is.EqualTo(960).Within(2));
        var reverse=hosts.Single(h=>h.Name=="Mac").Neighbours.Single();
        Assert.That((reverse.SourceStart,reverse.SourceEnd,reverse.DestStart,reverse.DestEnd),Is.EqualTo((10,90,20,80)));
        Assert.That(layout.DetectEdgeExit(BenQ,20,0),Is.Null);
        Assert.That(layout.DetectEdgeExit(BenQ,960,0)!.Destination,Is.SameAs(WinBig));
    }
    [Test]
    public void MissingBenQIdentifier_DoesNotFallBackToMacBook()
    {
        var(_,hosts)=Build();hosts[0].Neighbours[0]=new NeighbourConfig { Name="Mac",Direction=Direction.Down,SourceScreen=WinBig.Name,DestScreen="missing-id",Mirror=false };
        var layout=new ScreenLayout([WinBig,WinLaptop,MacBook,BenQ],hosts,0,[],NullLogger.Instance);
        Assert.That(layout.DetectEdgeExit(WinBig,1280,WinBig.Height-1),Is.Null);
    }
    [Test]
    public void RepeatedRoundTrips_NoScreenIdentityDrift()
    {
        var(layout,_)=Build();var x=1280;
        for(var i=0;i<100;i++)
        {
            var down=layout.DetectEdgeExit(WinBig,x,1439)!;Assert.That(down.Destination.Name,Is.EqualTo("benq"));
            var up=layout.DetectEdgeExit(BenQ,down.EntryX,0)!;Assert.That(up.Destination.Name,Is.EqualTo("win-external"));x=up.EntryX;
        }
        Assert.That(x,Is.EqualTo(1280).Within(2));
    }
    [Test]
    public void BenQ_LeftToMacBook_ThenRightBack_UsesOnlyMacHostCoordinates()
    {
        var mouse=new VirtualMouseState();
        mouse.EnterScreen(BenQ,[MacBook,BenQ],2,400,1.0m,new(){[MacBook.Name]=0.5m,[BenQ.Name]=1.0m});
        Assert.That(mouse.ApplyDelta(-10,0),Is.SameAs(BenQ));
        Assert.That(mouse.CurrentScreen,Is.SameAs(MacBook));Assert.That(mouse.MouseScale,Is.EqualTo(0.5m));
        mouse.ApplyDelta(20,0);
        Assert.That(mouse.CurrentScreen,Is.SameAs(BenQ));Assert.That(mouse.MouseScale,Is.EqualTo(1.0m));
        Assert.That(mouse.RemoteScreens.All(s=>s.Host=="Mac"),Is.True);
    }

}
