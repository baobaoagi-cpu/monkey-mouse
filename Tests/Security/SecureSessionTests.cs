// Modified 2026-09-13; GPL-2.0. Tests use memory only: no sockets, Keychain, DPAPI or helpers.
using System.IO.Pipelines;
using System.Security.Authentication;
using System.Security.Cryptography;
using Hydra.Security;
using Hydra.Relay;

namespace Tests.Security;

internal sealed class MemoryProtectedStore : IProtectedBlobStore
{
    private byte[]? _data;
    public bool FailWrites;
    private int _leased;
    private sealed class Lease(Action release) : IDisposable { public void Dispose() => release(); }
    public IDisposable AcquireLease()
    {
        if (Interlocked.CompareExchange(ref _leased,1,0)!=0) throw new IOException("Vault already leased");
        return new Lease(()=>Interlocked.Exchange(ref _leased,0));
    }
    public byte[]? Read() => _data?.ToArray();
    public void Write(byte[] data) { if (FailWrites) throw new IOException("simulated protected-store failure"); _data = data.ToArray(); }
}

internal sealed class MemoryDuplex(Stream read, Stream write) : Stream
{
    public bool Capture;
    public bool Tamper;
    public List<byte[]> Captured { get; } = [];
    public static (MemoryDuplex A, MemoryDuplex B) Pair()
    {
        var ab = new Pipe(new PipeOptions(pauseWriterThreshold: 2 * 1024 * 1024));
        var ba = new Pipe(new PipeOptions(pauseWriterThreshold: 2 * 1024 * 1024));
        return (new(ba.Reader.AsStream(), ab.Writer.AsStream()), new(ab.Reader.AsStream(), ba.Writer.AsStream()));
    }
    public override bool CanRead => true; public override bool CanWrite => true; public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] b, int o, int c) => read.Read(b, o, c);
    public override ValueTask<int> ReadAsync(Memory<byte> b, CancellationToken ct = default) => read.ReadAsync(b, ct);
    public override Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken ct) => read.ReadAsync(b, o, c, ct);
    public override void Write(byte[] b, int o, int c) => WriteAsync(b.AsMemory(o, c)).AsTask().GetAwaiter().GetResult();
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> b, CancellationToken ct = default)
    {
        var bytes = b.ToArray();
        if (Capture) Captured.Add(bytes.ToArray());
        if (Tamper && bytes.Length > 5) bytes[^1] ^= 1;
        await write.WriteAsync(bytes, ct); await write.FlushAsync(ct);
    }
    public async Task Replay(byte[] bytes) { await write.WriteAsync(bytes); await write.FlushAsync(); }
    public override void Flush() => write.Flush();
    public override Task FlushAsync(CancellationToken ct) => write.FlushAsync(ct);
    public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
    public override void SetLength(long v) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) { read.Dispose(); write.Dispose(); } base.Dispose(disposing); }
}

[TestFixture]
public class SecureSessionTests
{
    private static DeviceVault New() => DeviceVault.Create(new MemoryProtectedStore());
    internal static void Approve(DeviceVault local, DeviceVault remote, string alias, bool receive = true, bool send = true, bool clipboard = true)
    {
        var pending = local.BeginApproval(remote.Invitation(), remote.DeviceId, alias, receive, send, clipboard);
        local.Confirm(pending.Token);
    }
    private static async Task<(SecurePeerSession A, SecurePeerSession B, MemoryDuplex WireA)> Connect(DeviceVault a, DeviceVault b)
    {
        var (wa, wb) = MemoryDuplex.Pair();
        var tasks = new[] { SecurePeerSession.Authenticate(wa, a, b.DeviceId, false), SecurePeerSession.Authenticate(wb, b, a.DeviceId, true) };
        try { await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(8)); return (tasks[0].Result, tasks[1].Result, wa); }
        catch { wa.Dispose(); wb.Dispose(); foreach (var t in tasks.Where(t => t.Status == TaskStatus.RanToCompletion)) await t.Result.DisposeAsync(); throw; }
    }
    [Test]
    public void DistinctIdentityAndTrust_SurviveVaultReopen()
    {
        var store = new MemoryProtectedStore(); using var first = DeviceVault.Create(store); using var peer = New();
        Approve(first, peer, "Mac");
        var originalId = first.DeviceId; first.Dispose();
        using var reopened = DeviceVault.Open(store);
        using (Assert.EnterMultipleScope())
        { Assert.That(reopened.DeviceId, Is.EqualTo(originalId)); Assert.That(peer.DeviceId, Is.Not.EqualTo(originalId)); Assert.That(reopened.Find(peer.DeviceId)?.Alias, Is.EqualTo("Mac")); }
        Assert.Throws<IOException>(() => DeviceVault.Create(store));
    }
    [Test]
    public void Approval_RejectsWrongFullFingerprintAndConsumedToken()
    {
        using var a = New(); using var b = New();
        Assert.Throws<CryptographicException>(() => a.BeginApproval(b.Invitation(), new string('A',64), "Mac", true, false, true));
        var pending = a.BeginApproval(b.Invitation(), b.DeviceId, "Mac", true, false, true);
        Assert.That(a.Find(b.DeviceId), Is.Null);
        a.Confirm(pending.Token);
        Assert.Throws<InvalidOperationException>(() => a.Confirm(pending.Token));
    }
    [Test]
    public void Approval_ExpiresAndDoesNotPersistOnStoreFailure()
    {
        var now = DateTimeOffset.UtcNow; var store = new MemoryProtectedStore();
        using var a = DeviceVault.Create(store, () => now); using var b = New();
        var pending = a.BeginApproval(b.Invitation(), b.DeviceId, "Mac", true, false, true);
        now += TimeSpan.FromMinutes(3); Assert.Throws<InvalidOperationException>(() => a.Confirm(pending.Token));
        pending = a.BeginApproval(b.Invitation(), b.DeviceId, "Mac", true, false, true); store.FailWrites = true;
        Assert.Throws<IOException>(() => a.Confirm(pending.Token)); Assert.That(a.Find(b.DeviceId), Is.Null);
    }
    [Test]
    public void SameAliasDifferentKey_IsNotReplacementApproval()
    {
        using var a = New(); using var b = New(); using var impostor = New(); Approve(a,b,"Mac");
        Assert.Throws<InvalidOperationException>(() => a.BeginApproval(impostor.Invitation(), impostor.DeviceId, "Mac", true,true,true));
        Assert.That(a.Find(impostor.DeviceId), Is.Null);
    }
    [Test]
    public async Task MutualTls_TransfersTextAndKeyboard_WithoutHostnameIdentity()
    {
        using var a = New(); using var b = New(); Approve(a,b,"BenQ Mac"); Approve(b,a,"ASUS");
        var (sa,sb,_) = await Connect(a,b); await using var aa=sa; await using var bb=sb;
        var payload=MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("中文 hello"));
        Assert.That(await sa.Send(payload), Is.True);
        Assert.That(MessageSerializer.Decode((await sb.Receive())!).Deserialize<ClipboardPushMessage>().Text, Is.EqualTo("中文 hello"));
        Assert.That(await sb.Send(MessageSerializer.Encode(MessageKind.ClipboardPullResponse,new ClipboardPullResponseMessage("Mac 回傳"))),Is.True);
        Assert.That(MessageSerializer.Decode((await sa.Receive())!).Deserialize<ClipboardPullResponseMessage>().Text,Is.EqualTo("Mac 回傳"));
        TestContext.Out.WriteLine($"Negotiated protocol: {sa.NegotiatedProtocol}; transport: in-memory duplex pipes");
        Assert.That(sb.Alias, Is.EqualTo("ASUS"));
        Assert.That(sa.NegotiatedProtocol, Is.AnyOf(SslProtocols.Tls12,SslProtocols.Tls13));
    }
    [Test]
    public async Task OneSidedApproval_DoesNotCreateActiveSession()
    {
        using var a=New(); using var b=New(); Approve(a,b,"Mac");
        Assert.That(async () => await Connect(a,b), Throws.Exception);
        await Task.CompletedTask;
    }
    [Test]
    public async Task ImpostorWithSameClaimedNameButDifferentKey_FailsTls()
    {
        using var a=New(); using var b=New(); using var evil=New(); Approve(a,b,"Mac"); Approve(evil,a,"ASUS");
        var (wa,wb)=MemoryDuplex.Pair(); using var ca=wa; using var cb=wb;
        var attempt = Task.WhenAll(SecurePeerSession.Authenticate(wa,a,b.DeviceId,false),SecurePeerSession.Authenticate(wb,evil,a.DeviceId,true));
        Assert.That(async () => await attempt, Throws.Exception); await Task.CompletedTask;
    }
    [Test]
    public async Task CiphertextReplayInSameSession_IsRejected()
    {
        using var a=New(); using var b=New(); Approve(a,b,"Mac"); Approve(b,a,"ASUS");
        var (sa,sb,wire)=await Connect(a,b); await using var aa=sa; await using var bb=sb;
        wire.Capture=true;
        await sa.Send(MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("not plaintext on wire")));
        Assert.That(await sb.Receive(), Is.Not.Null);
        Assert.That(System.Text.Encoding.UTF8.GetString(wire.Captured.SelectMany(x=>x).ToArray()), Does.Not.Contain("not plaintext on wire"));
        foreach(var packet in wire.Captured.ToArray()) await wire.Replay(packet);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        Assert.That(async () => await sb.Receive(timeout.Token), Throws.Exception);
        Assert.That(sb.IsOpen, Is.False);
    }
    [Test]
    public async Task TamperedCiphertext_IsRejected()
    {
        using var a=New(); using var b=New(); Approve(a,b,"Mac"); Approve(b,a,"ASUS");
        var (sa,sb,wire)=await Connect(a,b); await using var aa=sa; await using var bb=sb;
        wire.Tamper=true; await sa.Send(MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("secret")));
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));
        Assert.That(async () => await sb.Receive(timeout.Token), Throws.Exception);
        Assert.That(sb.IsOpen, Is.False);
    }
    [Test]
    public async Task Revocation_ClosesExistingSessionAndPersists()
    {
        var store = new MemoryProtectedStore(); using var a=DeviceVault.Create(store); using var b=New(); Approve(a,b,"Mac"); Approve(b,a,"ASUS");
        var (sa,sb,_)=await Connect(a,b); await using var aa=sa; await using var bb=sb;
        a.Revoke(b.DeviceId); Assert.That(sa.IsOpen, Is.False);
        a.Dispose(); using var reopened=DeviceVault.Open(store); Assert.That(reopened.Find(b.DeviceId), Is.Null);
        Assert.That(async () => await sa.Send(MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("denied"))), Throws.Exception);
    }
    [Test]
    public async Task CapabilityAndFilePowerPolicy_AppliedOnAuthenticatedSession()
    {
        using var a=New(); using var b=New(); Approve(a,b,"Mac",send:false); Approve(b,a,"ASUS");
        var (sa,sb,_)=await Connect(a,b); await using var aa=sa; await using var bb=sb;
        foreach (var kind in Enum.GetValues<MessageKind>().Where(k=> k.ToString().StartsWith("File",StringComparison.Ordinal)).Concat([MessageKind.LockScreen,MessageKind.ScreensaverSync,MessageKind.ActivityPing,MessageKind.KeyEvent]))
            Assert.That(await sa.Send([(byte)kind,123,125]),Is.False);
        Assert.That(await sa.Send(MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("text","primary",[1,2],"<b>unsafe</b>",[3]))),Is.True);
        var received=MessageSerializer.Decode((await sb.Receive())!).Deserialize<ClipboardPushMessage>();
        using(Assert.EnterMultipleScope()){Assert.That(received.Text,Is.EqualTo("text"));Assert.That(received.ImagePng,Is.Null);Assert.That(received.Html,Is.Null);Assert.That(received.Rtf,Is.Null);}
    }
    private static async Task RawFrame(SecurePeerSession session, ulong sequence, byte[] payload, int? length = null)
    {
        var stream=(System.Net.Security.SslStream)typeof(SecurePeerSession).GetField("_tls",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(session)!;
        var header=new byte[12];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header,length??payload.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(header.AsSpan(4),sequence);
        await stream.WriteAsync(header);if(payload.Length>0)await stream.WriteAsync(payload);await stream.FlushAsync();
    }
    [Test]
    public async Task ReceiverPolicy_StopsCompromisedApprovedSender_BypassingItsLocalFilter()
    {
        using var a=New();using var b=New();Approve(a,b,"Mac");Approve(b,a,"ASUS",receive:false);
        var(sa,sb,_)=await Connect(a,b);await using var aa=sa;await using var bb=sb;
        ulong sequence=0;
        foreach(var payload in new[]{
            MessageSerializer.Encode(MessageKind.FileTransferRequest,new FileTransferRequestMessage()),
            MessageSerializer.Encode(MessageKind.KeyEvent,new KeyEventMessage(Hydra.Keyboard.KeyEventType.KeyDown,Hydra.Keyboard.KeyModifiers.None,'x',null)),
            MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("text",null,[1],"<b>bad</b>",[2])),
            MessageSerializer.Encode(MessageKind.LockScreen,new LockScreenMessage(0))})
        { await RawFrame(sa,sequence++,payload);Assert.That(await sb.Receive(),Is.Null); }
    }
    [Test]
    public async Task DuplicateApplicationSequence_ClosesSession()
    {
        using var a=New();using var b=New();Approve(a,b,"Mac");Approve(b,a,"ASUS");
        var(sa,sb,_)=await Connect(a,b);await using var aa=sa;await using var bb=sb;
        var payload=MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("hello"));
        await RawFrame(sa,0,payload);Assert.That(await sb.Receive(),Is.Not.Null);
        await RawFrame(sa,0,payload);Assert.That(async()=>await sb.Receive(),Throws.TypeOf<IOException>());
    }
    [Test]
    public async Task OversizedFrame_RejectedBeforePayloadAllocation()
    {
        using var a=New();using var b=New();Approve(a,b,"Mac");Approve(b,a,"ASUS");
        var(sa,sb,_)=await Connect(a,b);await using var aa=sa;await using var bb=sb;
        await RawFrame(sa,0,[],int.MaxValue);Assert.That(async()=>await sb.Receive(),Throws.TypeOf<IOException>());
    }
    [Test]
    public async Task OldCiphertext_CannotReplayIntoFreshTlsSession()
    {
        using var a=New();using var b=New();Approve(a,b,"Mac");Approve(b,a,"ASUS");
        var(firstA,firstB,firstWire)=await Connect(a,b);firstWire.Capture=true;
        await firstA.Send(MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("old")));await firstB.Receive();
        var packets=firstWire.Captured.ToArray();await firstA.DisposeAsync();await firstB.DisposeAsync();
        var(nextA,nextB,nextWire)=await Connect(a,b);await using var aa=nextA;await using var bb=nextB;
        foreach(var bytes in packets)await nextWire.Replay(bytes);
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));
        Assert.That(async()=>await nextB.Receive(timeout.Token),Throws.Exception);Assert.That(nextB.IsOpen,Is.False);
    }
    [Test]
    public async Task Adapter_UsesApprovedAlias_AndDisconnectsOnRevocation()
    {
        using var a=New();using var b=New();Approve(a,b,"Mac");Approve(b,a,"ASUS");
        var(sa,sb,_)=await Connect(a,b);
        await using var adapterA=new SecureRelayAdapter(sa);await using var adapterB=new SecureRelayAdapter(sb);
        var received=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnected=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        adapterB.MessageReceived+=(host,_,_)=>{received.TrySetResult(host);return Task.CompletedTask;};
        adapterB.Disconnected+=()=>{disconnected.TrySetResult();return Task.CompletedTask;};
        var runA=adapterA.Run();var runB=adapterB.Run();
        adapterA.Send(["Mac"],MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("hello")));
        Assert.That(await received.Task.WaitAsync(TimeSpan.FromSeconds(3)),Is.EqualTo("ASUS"));
        b.Revoke(a.DeviceId);await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.That(adapterB.IsConnected,Is.False);await Task.WhenAll(runA,runB).WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Test]
    public async Task VerifiedSession_ReachesExistingHydraSlaveClipboardHandler()
    {
        using var a=New();using var b=New();Approve(a,b,"Mac");Approve(b,a,"ASUS");
        var(sa,sb,_)=await Connect(a,b);
        using var master=new MasterRelayConnection(Tests.Setup.TransitionTestHelper.Profile("ASUS",new Hydra.Config.HydraConfig{Mode=Hydra.Config.Mode.Master}),Microsoft.Extensions.Logging.Abstractions.NullLogger<RelayConnection>.Instance,new WorldState());
        var clipboard=new Tests.Setup.FakeClipboardSync();using var slave=new Tests.Setup.TestableSlaveRelay(clipboard:clipboard);
        using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var mr=master.RunApprovedSession(sa,cancel.Token);var sr=slave.RunApprovedSession(sb,cancel.Token);
        master.Send(["Mac"],MessageSerializer.Encode(MessageKind.MasterConfig,new MasterConfigMessage(null)));
        master.Send(["Mac"],MessageSerializer.Encode(MessageKind.ClipboardPush,new ClipboardPushMessage("四螢幕文字")));
        while(clipboard.Text!="四螢幕文字")await Task.Delay(10,cancel.Token);
        Assert.That(clipboard.Text,Is.EqualTo("四螢幕文字"));
        cancel.Cancel();await Task.WhenAll(mr,sr).WaitAsync(TimeSpan.FromSeconds(3));
    }
    [Test]
    public async Task LegacyHostnameEntryPoints_CannotDispatchAnything()
    {
        using var relay=new RelayConnection(Tests.Setup.TransitionTestHelper.Profile("local"),Microsoft.Extensions.Logging.Abstractions.NullLogger<RelayConnection>.Instance,new WorldState());
        var dispatched=0;
        relay.MessageReceived+=(_,_,_)=>{dispatched++;return Task.CompletedTask;};
        relay.PeersChanged+=_=>{dispatched++;return Task.CompletedTask;};
        await relay.Receive("ASUS","192.168.0.17",[1,2,3]);await relay.Peers(["ASUS"]);await relay.Kicked("pretend");
        Assert.That(dispatched,Is.Zero);Assert.That(Hydra.Config.ReviewBuildPolicy.RuntimeEnabled,Is.False);
    }

}
