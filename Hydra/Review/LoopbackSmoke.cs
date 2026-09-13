// Monkey Mouse, modified 2026-09-13; GPL-2.0. Synthetic peers, real loopback TCP/TLS, no OS input/clipboard.
using System.Security.Cryptography;
using Hydra.Config;
using Hydra.Keyboard;
using Hydra.Relay;
using Hydra.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hydra.Review;

public sealed record LoopbackSmokeResult(string Status, int ReceivedAtAsus, int ReceivedAtMac, string Transport, string Scope);

public static class LoopbackSmoke
{
    public static async Task<LoopbackSmokeResult> Run(CancellationToken cancel = default)
    {
        using var aStore = new EphemeralStore(); using var bStore = new EphemeralStore();
        using var a = DeviceVault.Create(aStore); using var b = DeviceVault.Create(bStore);
        // This is a synthetic test fixture, not approval of a remote device or a persistent identity.
        PairingWorkflow.Pair(a, b.Invitation(), "Mac", "send-input", new StringReader(b.DeviceId + "\nPAIR\n"), TextWriter.Null);
        PairingWorkflow.Pair(b, a.Invitation(), "ASUS", "receive-input", new StringReader(a.DeviceId + "\nPAIR\n"), TextWriter.Null);
        using var ar = NewRelay("ASUS"); using var br = NewRelay("Mac");
        var mac = new List<MessageKind>(); var asus = new List<MessageKind>();
        var macDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var asusDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        br.MessageReceived += (host, kind, body) =>
        {
            if (host != "ASUS") throw new IOException("Untrusted alias");
            mac.Add(kind); if (kind == MessageKind.ClipboardPush) macDone.TrySetResult();
            return Task.CompletedTask;
        };
        ar.MessageReceived += (host, kind, body) =>
        {
            if (host != "Mac") throw new IOException("Untrusted alias");
            asus.Add(kind); if (kind == MessageKind.ClipboardPush) asusDone.TrySetResult();
            return Task.CompletedTask;
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var host = new SecureSocketHost(b, a.DeviceId, br);
        var server = host.RunOnce(timeout.Token);
        var client = SecureSocketController.RunClient(a, b.DeviceId, ar, host.Endpoint, timeout.Token);
        try
        {
            while (!ar.IsConnected || !br.IsConnected)
            {
                if (server.IsCompleted || client.IsCompleted) { await Task.WhenAll(server, client); throw new IOException("Connection ended before ready"); }
                await Task.Delay(5, timeout.Token);
            }
            ar.Send(["Mac"], MessageSerializer.Encode(MessageKind.KeyEvent, new KeyEventMessage(KeyEventType.KeyDown, KeyModifiers.None, 'x', null)));
            ar.Send(["Mac"], MessageSerializer.Encode(MessageKind.KeyEvent, new KeyEventMessage(KeyEventType.KeyUp, KeyModifiers.None, 'x', null)));
            ar.Send(["Mac"], MessageSerializer.Encode(MessageKind.MouseMoveDelta, new MouseMoveDeltaMessage(5, 3)));
            // Forbidden traffic must not reach the sink. Clipboard is a FIFO completion barrier.
            ar.Send(["Mac"], MessageSerializer.Encode(MessageKind.FileTransferRequest, new FileTransferRequestMessage()));
            ar.Send(["Mac"], MessageSerializer.Encode(MessageKind.LockScreen, new LockScreenMessage(0)));
            ar.Send(["Mac"], [255, 123, 125]);
            ar.Send(["Mac"], MessageSerializer.Encode(MessageKind.ClipboardPush, new ClipboardPushMessage("Monkey Mouse 四屏 smoke")));
            br.Send(["ASUS"], MessageSerializer.Encode(MessageKind.ClipboardPush, new ClipboardPushMessage("text return")));
            await Task.WhenAll(macDone.Task, asusDone.Task).WaitAsync(timeout.Token);
            if (!mac.SequenceEqual([MessageKind.KeyEvent, MessageKind.KeyEvent, MessageKind.MouseMoveDelta, MessageKind.ClipboardPush]) ||
                !asus.SequenceEqual([MessageKind.ClipboardPush])) throw new IOException("Unexpected dispatch sequence");
            return new("PASS", asus.Count, mac.Count, "TCP loopback + pinned mutual TLS + SecureRelayAdapter", "Synthetic input/message sinks; no native input, OS clipboard, persistent vault or LAN");
        }
        finally
        {
            timeout.Cancel(); host.Dispose();
            await Task.WhenAll(server, client);
        }
    }

    private static RelayConnection NewRelay(string name) => new(
        new HydraProfile(new HydraConfigFile { Name = name }, new HydraConfig { Mode = Mode.Master }, null),
        NullLogger<RelayConnection>.Instance, new WorldState());

    private sealed class EphemeralStore : IProtectedBlobStore, IDisposable
    {
        private byte[]? _data; private int _leased;
        public byte[]? Read() => _data?.ToArray();
        public void Write(byte[] data) { if (_data != null) CryptographicOperations.ZeroMemory(_data); _data = data.ToArray(); }
        public IDisposable AcquireLease()
        {
            if (Interlocked.CompareExchange(ref _leased, 1, 0) != 0) throw new IOException("Already open");
            return new Lease(() => Interlocked.Exchange(ref _leased, 0));
        }
        public void Dispose() { if (_data != null) CryptographicOperations.ZeroMemory(_data); }
        private sealed class Lease(Action release) : IDisposable { public void Dispose() => release(); }
    }
}
