// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
using System.Text;
using Hydra.Config;
using Hydra.Relay;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Setup;

namespace Tests.Relay;

[TestFixture]
public class SafePolicyTests
{
    private static readonly MessageKind[] FileKinds = Enum.GetValues<MessageKind>()
        .Where(k => k.ToString().StartsWith("File", StringComparison.Ordinal)).ToArray();

    private sealed class ProbeRelay(IHydraProfile profile)
        : MasterRelayConnection(profile, NullLogger<RelayConnection>.Instance, new WorldState())
    {
        public int Outbound;
        protected override void OnSent(string[] targets, byte[] payload) => Outbound++;
        public Task Inject(MessageKind kind) => OnReceive("peer", kind, "invalid json"u8.ToArray());
    }

    [Test]
    public void MissingSettings_DenySensitiveFeatures_IncludingIdleProfile()
    {
        foreach (var config in new HydraConfig?[] { null, new() { Mode = Mode.Master }, new() { Mode = Mode.Slave } })
        {
            var profile = TransitionTestHelper.Profile("local", config);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile.EnableFileTransfer, Is.False);
                Assert.That(profile.SyncScreensaver, Is.False);
                Assert.That(profile.ScreenLockPropagation, Is.False);
                Assert.That(profile.AllowRemoteLock, Is.False);
                Assert.That(profile.AllowRemoteWake, Is.False);
                Assert.That(profile.AutoUpdate, Is.False);
            }
        }
    }

    [TestCaseSource(nameof(FileKinds))]
    public async Task Relay_DropsEveryFileMessage_BothDirectionsBeforeDispatch(MessageKind kind)
    {
        using var relay = new ProbeRelay(TransitionTestHelper.Profile("local", new HydraConfig { Mode = Mode.Master }));
        var inbound = 0;
        relay.MessageReceived += (_, _, _) => { inbound++; return Task.CompletedTask; };
        // Invalid JSON deliberately proves refusal occurs before parsing file paths or content.
        relay.Send(["peer"], [(byte)kind, 255]);
        await relay.Inject(kind);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(relay.Outbound, Is.Zero);
            Assert.That(inbound, Is.Zero);
        }
    }

    [TestCase(MessageKind.ScreensaverSync)]
    [TestCase(MessageKind.ActivityPing)]
    [TestCase(MessageKind.LockScreen)]
    public async Task Relay_DropsSystemControl_BothDirections(MessageKind kind)
    {
        using var relay = new ProbeRelay(TransitionTestHelper.Profile("local", new HydraConfig { Mode = Mode.Master }));
        var inbound = 0;
        relay.MessageReceived += (_, _, _) => { inbound++; return Task.CompletedTask; };
        relay.Send(["peer"], [(byte)kind, 123, 125]);
        await relay.Inject(kind);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(relay.Outbound, Is.Zero);
            Assert.That(inbound, Is.Zero);
        }
    }

    [TestCaseSource(nameof(FileKinds))]
    public async Task ExplicitFileOptIn_RetainsRelayProtocol(MessageKind kind)
    {
        using var relay = new ProbeRelay(TransitionTestHelper.Profile("local", new HydraConfig { Mode = Mode.Master, EnableFileTransfer = true }));
        var inbound = 0;
        relay.MessageReceived += (_, _, _) => { inbound++; return Task.CompletedTask; };
        relay.Send(["peer"], [(byte)kind, 123, 125]);
        await relay.Inject(kind);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(relay.Outbound, Is.EqualTo(1));
            Assert.That(inbound, Is.EqualTo(1));
        }
    }

    [TestCase(MessageKind.ClipboardPush)]
    [TestCase(MessageKind.ClipboardPullResponse)]
    [TestCase(MessageKind.KeyEvent)]
    [TestCase(MessageKind.MouseMove)]
    public async Task EssentialTraffic_StillPassesPolicy(MessageKind kind)
    {
        using var relay = new ProbeRelay(TransitionTestHelper.Profile("local", new HydraConfig { Mode = Mode.Master }));
        var inbound = 0;
        relay.MessageReceived += (_, _, _) => { inbound++; return Task.CompletedTask; };
        relay.Send(["peer"], [(byte)kind, 123, 125]);
        await relay.Inject(kind);
        Assert.That((relay.Outbound, inbound), Is.EqualTo((1, 1)));
    }

    [Test]
    public async Task Slave_RejectsKnownMastersLockScreensaverAndActivity_ByDefault()
    {
        var sync = new FakeScreenSaverSync();
        using var slave = new TestableSlaveRelay(screenSaverSync: sync);
        await slave.SimulateConnected();
        await slave.SimulateMasterConfig("known-master");
        await slave.SimulateReceive("known-master", MessageKind.LockScreen, "{\"millisecondsSinceLastInput\":0}");
        await slave.SimulateReceive("known-master", MessageKind.ScreensaverSync, "{\"active\":true}");
        await slave.SimulateReceive("known-master", MessageKind.ScreensaverSync, "{\"active\":false}");
        await slave.SimulateReceive("known-master", MessageKind.ActivityPing, "{}");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sync.LockScreenCalled, Is.False);
            Assert.That(sync.ActivateCount, Is.Zero);
            Assert.That(sync.DeactivateCount, Is.Zero);
            Assert.That(sync.ResetIdleTimerCalled, Is.False);
        }
    }

    [Test]
    public async Task Dormant_DefaultPolicyDoesNotWakeDisplayOnRemoteInput()
    {
        var sync = new FakeScreenSaverSync();
        using var slave = new TestableSlaveRelay(screenSaverSync: sync);
        await slave.SimulateConnected();
        await slave.SimulateMasterConfig("master");
        await slave.Dormancy.Enter();
        await slave.SimulateReceive("master", MessageKind.MouseMove, "{\"screen\":\"home:0\",\"x\":1,\"y\":1}");
        await slave.SimulateReceive("master", MessageKind.ActivityPing, "{}");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sync.WakeDisplayCount, Is.Zero);
            Assert.That(slave.Output.TotalInjections, Is.Zero);
            Assert.That(slave.Dormancy.RequestWake(), Is.True); // no remote wake timer was armed
        }
    }

    [TestCase(Mode.Master)]
    [TestCase(Mode.Slave)]
    public async Task SyncOff_DoesNotResetIdleOrBroadcast_FromAnyActivityPath(Mode mode)
    {
        var sync = new FakeScreenSaverSync();
        var sender = new FakeRelay();
        var world = new WorldState();
        await world.AddMaster("master", new MasterConfigMessage(null));
        await world.SetPeerScreens("slave", []);
        var tracker = new ActivityTracker(TransitionTestHelper.Profile("local", new HydraConfig { Mode = mode }),
            new Lazy<IRelaySender>(() => sender), world, sync, NullLogger<ActivityTracker>.Instance, () => 60_000);
        await tracker.LocalActivity();
        await tracker.RemoteActivity("peer");
        await tracker.IncomingPing();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sync.ResetIdleTimerCalled, Is.False);
            Assert.That(sender.Sent, Is.Empty);
            Assert.That(tracker.MsSinceLocalActivity, Is.Zero); // local bookkeeping still works
        }
    }
}
