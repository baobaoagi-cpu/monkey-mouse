// Monkey Mouse, modified 2026-09-13; GPL-2.0. Explicit loopback transport; never initializes OS input.
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Hydra.Relay;

namespace Hydra.Security;

/// <summary>A single approved peer over real TCP/TLS, connected to the existing relay lifecycle.
/// Loopback is enforced for this development slice. LAN control requires a separately reviewed entry point.</summary>
public sealed class SecureSocketHost : IDisposable
{
    private readonly TcpListener _listener;
    private readonly DeviceVault _vault;
    private readonly string _peerId;
    private readonly RelayConnection _relay;
    private readonly CancellationTokenSource _stop = new();
    private int _running;
    private int _disposed;
    public IPEndPoint Endpoint { get; }

    public SecureSocketHost(DeviceVault vault, string peerId, RelayConnection relay, int port = 0)
    {
        SecureSocketController.RequireApproval(vault, peerId);
        if (port is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        _vault = vault; _peerId = peerId; _relay = relay;
        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start(1);
        Endpoint = (IPEndPoint)_listener.LocalEndpoint;
    }

    public async Task RunOnce(CancellationToken cancel = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("An accept/session is already active");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, _stop.Token);
        try
        {
            SecureSocketController.RequireApproval(_vault, _peerId);
            using var tcp = await _listener.AcceptTcpClientAsync(linked.Token);
            tcp.NoDelay = true;
            await using var session = await SecurePeerSession.Authenticate(tcp.GetStream(), _vault, _peerId, true, linked.Token);
            await _relay.RunApprovedSession(session, linked.Token);
        }
        catch (Exception ex) when (linked.IsCancellationRequested && ex is OperationCanceledException or SocketException or ObjectDisposedException) { }
        finally { Volatile.Write(ref _running, 0); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stop.Cancel(); _listener.Stop();
        // The token remains valid while an in-flight RunOnce finishes cleanup.
    }
}

public static class SecureSocketController
{
    internal static void RequireApproval(DeviceVault vault, string peerId)
    {
        if (vault.Find(peerId) is null) throw new AuthenticationException("Peer must be approved before opening a socket");
    }

    public static async Task RunClient(DeviceVault vault, string peerId, RelayConnection relay,
        IPEndPoint endpoint, CancellationToken cancel = default)
    {
        RequireApproval(vault, peerId);
        if (!IPAddress.IsLoopback(endpoint.Address) || endpoint.Port is < 1 or > 65535)
            throw new ArgumentException("This development controller permits explicit loopback endpoints only", nameof(endpoint));
        using var tcp = new TcpClient(endpoint.AddressFamily) { NoDelay = true };
        using (var connect = CancellationTokenSource.CreateLinkedTokenSource(cancel))
        {
            connect.CancelAfter(TimeSpan.FromSeconds(5));
            await tcp.ConnectAsync(endpoint.Address, endpoint.Port, connect.Token);
        }
        await using var session = await SecurePeerSession.Authenticate(tcp.GetStream(), vault, peerId, false, cancel);
        await relay.RunApprovedSession(session, cancel);
    }
}
