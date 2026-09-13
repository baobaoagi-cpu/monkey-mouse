// Modified 2026-09-13; GPL-2.0. See MODIFICATIONS.md.
using System.Text;
using Hydra.Platform;
using Hydra.Relay;
using Hydra.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Setup;

namespace Tests.Security;

[TestFixture]
public class TextOnlyTests
{
    [Test]
    public void PlatformRead_DoesNotReadNonTextFormatsOrResurrectFallback()
    {
        var clipboard=new FakeClipboardSync(); clipboard.SetText("文字");clipboard.SetupImage([1]);clipboard.SetupHtml("<b>bad</b>");clipboard.SetupRtf([2]);
        var snap=ClipboardUtils.ReadWithFallback(clipboard,new("old",null,[3],"old html",[4]),NullLogger.Instance,"test");
        using(Assert.EnterMultipleScope())
        { Assert.That(snap.Text,Is.EqualTo("文字"));Assert.That(snap.ImagePng,Is.Null);Assert.That(snap.Html,Is.Null);Assert.That(snap.Rtf,Is.Null);
          Assert.That(clipboard.GetImagePngCallCount,Is.Zero);Assert.That(clipboard.GetHtmlCallCount,Is.Zero);Assert.That(clipboard.GetRtfCallCount,Is.Zero); }
        clipboard.SetClipboard(new(null,null,[5]));
        Assert.That(ClipboardUtils.ReadWithFallback(clipboard,snap,NullLogger.Instance,"test").Text,Is.Null);
    }
    [TestCase("imagePng","\"AQI=\"")]
    [TestCase("html","\"<b>content</b>\"")]
    [TestCase("rtf","\"AQI=\"")]
    [TestCase("files","[\"secret.txt\"]")]
    [TestCase("uris","[\"file:///secret\"]")]
    public void Receive_RejectsRichAndFileFields(string property,string json)
    {
        var payload=new[]{(byte)MessageKind.ClipboardPush}.Concat(Encoding.UTF8.GetBytes("{\"text\":\"text\",\""+property+"\":"+json+"}")).ToArray();
        Assert.That(SecureMessagePolicy.Filter(payload,false),Is.Null);
    }
    [TestCase("{\"text\":\"a\",\"Text\":\"b\"}")]
    [TestCase("{\"text\":[]}")]
    [TestCase("{\"text\":\"a\",\"unknown\":null}")]
    public void Receive_RejectsAmbiguousOrUnrecognizedClipboard(string json)
    {
        Assert.That(SecureMessagePolicy.Filter(new[]{(byte)MessageKind.ClipboardPush}.Concat(Encoding.UTF8.GetBytes(json)).ToArray(),false),Is.Null);
    }
    [Test]
    public async Task SlaveHandler_NeverWritesRichClipboardEvenIfBypassedInUnitTest()
    {
        var clipboard=new FakeClipboardSync(); using var slave=new TestableSlaveRelay(clipboard:clipboard);
        await slave.SimulateConnected();await slave.SimulateMasterConfig("master");
        var payload=MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("plain",null,[1],"<b>bad</b>",[2]));
        await slave.SimulateReceive("master",MessageKind.ClipboardPush,MessageSerializer.Decode(payload).Json);
        using(Assert.EnterMultipleScope())
        { Assert.That(clipboard.Text,Is.EqualTo("plain"));Assert.That(clipboard.ImagePng,Is.Null);Assert.That(clipboard.Html,Is.Null);Assert.That(clipboard.Rtf,Is.Null); }
    }
}
