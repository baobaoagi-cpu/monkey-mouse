// Modified 2026-09-13; GPL-2.0. Synthetic measurements; no claim of actual hardware dimensions.
using Hydra.Config;
using Hydra.Screen;

namespace Tests.Screen;

[TestFixture]
public class FourScreenBuilderTests
{
    private static FourScreenMeasurements Complete()=>new(new("win-external",-2560,0,2560,1440,1),new("win-laptop",0,0,1920,1080,1.25m),
        new("macbook",0,0,1512,982,2),new("benq",1512,0,1920,1080,1),"HydraReported");
    [Test]
    public void MeasuredRoles_ProduceOnlyTheIntendedCrossMachineEdges()
    {
        var config=FourScreenProfileBuilder.Build(Complete());
        var forward=config.Hosts.Single(h=>h.Name=="ASUS").Neighbours.Single();
        var reverse=config.Hosts.Single(h=>h.Name=="Mac").Neighbours.Single();
        using(Assert.EnterMultipleScope())
        {Assert.That((forward.SourceScreen,forward.DestScreen,forward.Direction),Is.EqualTo(("win-external","benq",Direction.Down)));
         Assert.That((reverse.SourceScreen,reverse.DestScreen,reverse.Direction),Is.EqualTo(("benq","win-external",Direction.Up)));
         Assert.That(config.EnableFileTransfer||config.SyncScreensaver||config.AllowRemoteLock||config.AllowRemoteWake,Is.False);}
    }
    [Test]
    public void MissingDisplayIdOrScale_CannotGenerateConfiguration()
    {
        var data=Complete();
        Assert.Throws<InvalidOperationException>(()=>FourScreenProfileBuilder.Build(data with {WindowsExternal=data.WindowsExternal with {ScreenId=null}}));
        Assert.Throws<InvalidOperationException>(()=>FourScreenProfileBuilder.Build(data with {WindowsLaptop=data.WindowsLaptop with {Scale=null}}));
    }
    [Test]
    public void UnconfirmedCoordinateSpace_AndReversedMacOrder_AreRejected()
    {
        var data=Complete();
        Assert.Throws<InvalidOperationException>(()=>FourScreenProfileBuilder.Build(data with {CoordinateSpace=null}));
        Assert.Throws<InvalidOperationException>(()=>FourScreenProfileBuilder.Build(data with {MacBook=data.BenQ,BenQ=data.MacBook}));
    }
}
