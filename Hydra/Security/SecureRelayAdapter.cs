// Modified 2026-09-13; GPL-2.0. See MODIFICATIONS.md.
using System.Threading.Channels;
using Hydra.Relay;

namespace Hydra.Security;

// Bridges an authenticated single peer into Hydra's existing routing interface.
// Alias is local trusted metadata, never a hostname supplied with incoming packets.
public sealed class SecureRelayAdapter : IRelaySender, IAsyncDisposable
{
    private readonly SecurePeerSession _session;
    private readonly Channel<byte[]> _outbound = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(64)
    { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    private readonly CancellationTokenSource _stop = new();
    private int _started;
    private int _disconnected;
    private Task? _run;
    public bool IsConnected => _session.IsOpen && !_stop.IsCancellationRequested;
    public event Func<string[], Task>? PeersChanged;
    public event Func<string, MessageKind, ReadOnlyMemory<byte>, Task>? MessageReceived;
    public event Func<Task>? Disconnected;
    public SecureRelayAdapter(SecurePeerSession session) { _session = session; _session.Closed += Stop; }
    public void Send(string[] targetHosts, byte[] payload)
    {
        if (!IsConnected || targetHosts.Length != 1 || !targetHosts[0].Equals(_session.Alias, StringComparison.OrdinalIgnoreCase)) return;
        if (!_outbound.Writer.TryWrite(payload.ToArray())) Stop(); // never silently drop held-key transitions
    }
    public Task Run(CancellationToken cancel = default)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0) throw new InvalidOperationException("Adapter already started");
        _run = RunCore(cancel); return _run;
    }
    private async Task RunCore(CancellationToken cancel)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, _stop.Token);
        try
        {
            if (PeersChanged != null) await PeersChanged([_session.Alias]);
            var sender = SendLoop(linked.Token); var receiver = ReceiveLoop(linked.Token);
            await Task.WhenAny(sender, receiver); linked.Cancel();
            await _session.DisposeAsync();
            await Task.WhenAll(sender, receiver);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or System.Security.Authentication.AuthenticationException or ObjectDisposedException) { }
        finally
        {
            Stop(); await _session.DisposeAsync();
            if (Interlocked.Exchange(ref _disconnected, 1) == 0 && Disconnected != null) await Disconnected();
        }
    }
    private async Task SendLoop(CancellationToken cancel)
    {
        await foreach (var bytes in _outbound.Reader.ReadAllAsync(cancel)) await _session.Send(bytes, cancel);
    }
    private async Task ReceiveLoop(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            var bytes = await _session.Receive(cancel);
            if (bytes is null || !IsConnected) continue;
            var decoded = MessageSerializer.Decode(bytes);
            if (MessageReceived != null) await MessageReceived(_session.Alias, decoded.Kind, decoded.Bytes);
        }
    }
    private void Stop() { _stop.Cancel(); _outbound.Writer.TryComplete(); }
    public async ValueTask DisposeAsync()
    {
        Stop(); await _session.DisposeAsync();
        if (_run is not null) await _run;
        _session.Closed -= Stop;
    }
}
