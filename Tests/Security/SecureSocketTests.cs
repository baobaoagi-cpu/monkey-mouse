// Monkey Mouse, modified 2026-09-13; GPL-2.0. Real localhost TCP; fake OS sinks and memory-only identities.
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Hydra.Config;
using Hydra.Keyboard;
using Hydra.Relay;
using Hydra.Review;
using Hydra.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Setup;

namespace Tests.Security;

[TestFixture, Category("Loopback")]
public class SecureSocketTests
{
    private static DeviceVault New() => DeviceVault.Create(new MemoryProtectedStore());
    private static RelayConnection Relay(string name) => new(TransitionTestHelper.Profile(name), NullLogger<RelayConnection>.Instance, new WorldState());
    private static async Task Ready(RelayConnection a, RelayConnection b, CancellationToken token)
    {
        while (!a.IsConnected || !b.IsConnected) await Task.Delay(5, token);
    }
    [Test]
    public async Task CliSlice_PairsAndTransfersInputAndText_RejectingForbiddenMessages()
    {
        var result = await LoopbackSmoke.Run();
        Assert.That((result.Status, result.ReceivedAtAsus, result.ReceivedAtMac), Is.EqualTo(("PASS", 1, 4)));
    }
    [Test]
    public void UnapprovedHost_RefusesBeforeOpeningListener()
    {
        using var a=New(); using var b=New(); using var relay=Relay("Mac");
        Assert.Throws<AuthenticationException>(() => new SecureSocketHost(b,a.DeviceId,relay));
    }
    [Test]
    public void UnapprovedClient_RefusesBeforeConnection()
    {
        using var a=New(); using var b=New(); using var relay=Relay("ASUS");
        Assert.That(async()=>await SecureSocketController.RunClient(a,b.DeviceId,relay,new IPEndPoint(IPAddress.Loopback,1)),Throws.TypeOf<AuthenticationException>());
    }
    [TestCase("0.0.0.0")][TestCase("192.0.2.1")]
    public void LanOrWildcardEndpoint_IsRejected(string address)
    {
        using var a=New(); using var b=New(); using var relay=Relay("ASUS"); SecureSessionTests.Approve(a,b,"Mac");
        Assert.That(async()=>await SecureSocketController.RunClient(a,b.DeviceId,relay,new IPEndPoint(IPAddress.Parse(address),5000)),Throws.TypeOf<ArgumentException>());
    }
    [Test]
    public async Task DifferentIdentityEvenWithSameAlias_CannotCompleteTls()
    {
        using var a=New(); using var b=New(); using var impostor=New(); using var ar=Relay("ASUS"); using var br=Relay("Mac");
        SecureSessionTests.Approve(b,a,"ASUS"); SecureSessionTests.Approve(impostor,b,"Mac");
        using var host=new SecureSocketHost(b,a.DeviceId,br);
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var server=host.RunOnce(cancel.Token); var client=SecureSocketController.RunClient(impostor,b.DeviceId,ar,host.Endpoint,cancel.Token);
        Assert.That(async()=>await Task.WhenAll(server,client),Throws.Exception);
        Assert.That(ar.IsConnected||br.IsConnected,Is.False);
    }
    [Test]
    public async Task CancelWaitingAccept_ReleasesListenerPort()
    {
        using var a=New(); using var b=New(); using var relay=Relay("Mac"); SecureSessionTests.Approve(b,a,"ASUS");
        var host=new SecureSocketHost(b,a.DeviceId,relay);var endpoint=host.Endpoint;
        var run=host.RunOnce();host.Dispose();await run.WaitAsync(TimeSpan.FromSeconds(3));
        var replacement=new TcpListener(endpoint);try{replacement.Start();}finally{replacement.Stop();}
    }
    [Test]
    public async Task ExistingSlaveReceivesKeysMouseAndText_AndReleasesHeldKeyOnDisconnect()
    {
        using var a=New();using var b=New();SecureSessionTests.Approve(a,b,"Mac");SecureSessionTests.Approve(b,a,"ASUS");
        using var master=new MasterRelayConnection(TransitionTestHelper.Profile("ASUS",new HydraConfig{Mode=Mode.Master}),NullLogger<RelayConnection>.Instance,new WorldState());
        var clipboard=new FakeClipboardSync();using var slave=new TestableSlaveRelay(clipboard:clipboard);
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(8));using var host=new SecureSocketHost(b,a.DeviceId,slave);
        var server=host.RunOnce(cancel.Token);var client=SecureSocketController.RunClient(a,b.DeviceId,master,host.Endpoint,cancel.Token);
        try
        {
            await Ready(master,slave,cancel.Token);
            master.Send(["Mac"],MessageSerializer.Encode(MessageKind.MasterConfig,new MasterConfigMessage(null)));
            master.Send(["Mac"],MessageSerializer.Encode(MessageKind.KeyEvent,new KeyEventMessage(KeyEventType.KeyDown,KeyModifiers.None,'x',null)));
            master.Send(["Mac"],MessageSerializer.Encode(MessageKind.MouseMoveDelta,new MouseMoveDeltaMessage(4,7)));
            master.Send(["Mac"],MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("socket 四屏文字")));
            while(clipboard.Text!="socket 四屏文字")await Task.Delay(5,cancel.Token);
            Assert.That(slave.Output.Keys.Any(k=>k.Type==KeyEventType.KeyDown),Is.True);
            Assert.That(slave.Output.MoveCount,Is.EqualTo(1));
            b.Revoke(a.DeviceId);
            await Task.WhenAll(server,client).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(slave.Output.Keys.Any(k=>k.Type==KeyEventType.KeyUp),Is.True);
            Assert.That(master.IsConnected||slave.IsConnected,Is.False);
        }
        finally{cancel.Cancel();host.Dispose();await Task.WhenAll(server,client);}
    }
    [Test]
    public async Task Reconnect_ReusesRelayAfterCleanSessionShutdown()
    {
        using var a=New();using var b=New();SecureSessionTests.Approve(a,b,"Mac");SecureSessionTests.Approve(b,a,"ASUS");
        using var ar=Relay("ASUS");using var br=Relay("Mac");
        var messages=System.Threading.Channels.Channel.CreateUnbounded<string>();
        br.MessageReceived+=(_,kind,body)=>{if(kind==MessageKind.ClipboardPush)messages.Writer.TryWrite(System.Text.Json.JsonDocument.Parse(body).RootElement.GetProperty("text").GetString()!);return Task.CompletedTask;};
        using var host=new SecureSocketHost(b,a.DeviceId,br);
        for(var i=0;i<2;i++)
        {
            using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var server=host.RunOnce(cancel.Token);var client=SecureSocketController.RunClient(a,b.DeviceId,ar,host.Endpoint,cancel.Token);
            try
            {
                await Ready(ar,br,cancel.Token);
                ar.Send(["Mac"],MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("round-"+i)));
                Assert.That(await messages.Reader.ReadAsync(cancel.Token),Is.EqualTo("round-"+i));
            }
            finally{cancel.Cancel();await Task.WhenAll(server,client);}
            Assert.That(ar.IsConnected||br.IsConnected,Is.False);
        }
    }
    [Test]
    public async Task ReceiveInputCapability_DeniesKeyButAllowsTextBarrier()
    {
        using var a=New();using var b=New();SecureSessionTests.Approve(a,b,"Mac");SecureSessionTests.Approve(b,a,"ASUS",receive:false);
        using var ar=Relay("ASUS");using var br=Relay("Mac");var received=new List<MessageKind>();
        var barrier=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        br.MessageReceived+=(_,kind,_)=>{received.Add(kind);if(kind==MessageKind.ClipboardPush)barrier.TrySetResult();return Task.CompletedTask;};
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(8));using var host=new SecureSocketHost(b,a.DeviceId,br);
        var server=host.RunOnce(cancel.Token);var client=SecureSocketController.RunClient(a,b.DeviceId,ar,host.Endpoint,cancel.Token);
        try
        {
            await Ready(ar,br,cancel.Token);
            ar.Send(["Mac"],MessageSerializer.Encode(MessageKind.KeyEvent,new KeyEventMessage(KeyEventType.KeyDown,KeyModifiers.None,'x',null)));
            ar.Send(["Mac"],MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("barrier")));
            await barrier.Task.WaitAsync(cancel.Token);
            Assert.That(received,Is.EqualTo(new[]{MessageKind.ClipboardPush}));
        }
        finally{cancel.Cancel();host.Dispose();await Task.WhenAll(server,client);}
    }
}
