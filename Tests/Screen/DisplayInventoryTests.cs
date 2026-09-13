// Monkey Mouse, modified 2026-09-13; GPL-2.0. Synthetic enumeration; no system calls.
using Hydra.Platform;
using Hydra.Review;
namespace Tests.Screen;
[TestFixture]
public class DisplayInventoryTests
{
    [Test]
    public void NegativeOrigins_PreserveRawBoundsAndMatchEngineNormalization()
    {
        var result=DisplayInventory.FromDetected("ASUS",[
            new DetectedScreen(-2560,-100,2560,1440,"External","test-1","11"),
            new DetectedScreen(0,0,1920,1080,"Laptop","test-2","12")]);
        Assert.That(result.Displays.Select(d=>(d.EngineScreenId,d.X,d.Y,d.AdvertisedX,d.AdvertisedY)),
            Is.EqualTo(new[]{("ASUS:0",-2560,-100,0,0),("ASUS:1",0,0,2560,100)}));
    }
    [Test]
    public void SingleScreenName_ChangesWhenSecondDisplayConnects()
    {
        var screen=new DetectedScreen(0,0,1512,982,"Internal",null,"1");
        var single=DisplayInventory.FromDetected("Mac",[screen]);
        var dual=DisplayInventory.FromDetected("Mac",[screen,screen with{X=1512,PlatformId="2"}]);
        Assert.That(single.Displays[0].EngineScreenId,Is.EqualTo("Mac"));
        Assert.That(dual.Displays[0].EngineScreenId,Is.EqualTo("Mac:0"));
    }
}
