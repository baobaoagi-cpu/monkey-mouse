// Modified 2026-09-13; GPL-2.0. Legacy network transport removed. See MODIFICATIONS.md.
using Cathedral.Utils;
using Common.Interfaces;
using Hydra.Config;
using Hydra.Security;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging;

namespace Hydra.Relay;

public class RelayConnection(IHydraProfile profile, ILogger<RelayConnection> log, IWorldState peerState)
    : SimpleHostedService(log), IStyxClient, IRelaySender
{
    private SecureRelayAdapter? _approvedTransport;
    // Retained for derived test types/source compatibility; no legacy connection loop exists.
    protected virtual TimeSpan ReconnectDelay => TimeSpan.FromSeconds(Constants.ReconnectDelaySeconds);
    protected CancellationToken ConnectionToken { get; private set; }
    public bool IsConnected => _approvedTransport?.IsConnected ?? false;
    public event Func<string[], Task>? PeersChanged;
    public event Func<string, MessageKind, ReadOnlyMemory<byte>, Task>? MessageReceived;
    public event Func<Task>? Disconnected;

    public void Send(string[] targetHosts, byte[] payload)
    {
        if (payload.Length == 0 || !MessagePolicy.Allows(profile, (MessageKind)payload[0])) return;
        payload = SecureMessagePolicy.Filter(payload, sending: true) ?? [];
        if (payload.Length == 0) return;
        OnSent(targetHosts, payload);
        _approvedTransport?.Send(targetHosts, payload);
    }
    // All IStyxClient legacy entry points refuse unauthenticated identity assertions.
    public Task Receive(string sourceHost, string sourceIp, byte[] payload) => Task.CompletedTask;
    public Task Peers(string[] hostNames) => Task.CompletedTask;
    public Task Kicked(string reason) => Task.CompletedTask;

    public async Task RunApprovedSession(SecurePeerSession session, CancellationToken cancel = default)
    {
        if (_approvedTransport != null) throw new InvalidOperationException("A session is already attached");
        await using var transport = new SecureRelayAdapter(session);
        _approvedTransport = transport;
        transport.MessageReceived += OnReceive;
        transport.PeersChanged += OnPeers;
        transport.Disconnected += async () => { await OnDisconnected(); if (Disconnected != null) await Disconnected(); };
        ConnectionToken = cancel;
        try { await OnAuthenticated(); await transport.Run(cancel); }
        finally { _approvedTransport = null; }
    }
    protected virtual void OnSent(string[] targetHosts, byte[] payload) { }
    protected virtual async Task OnReceive(string sourceHost, MessageKind kind, ReadOnlyMemory<byte> body)
    {
        if (!MessagePolicy.Allows(profile, kind)) return;
        if (MessageReceived != null) await MessageReceived(sourceHost, kind, body);
    }
    protected virtual async Task OnPeers(string[] hostNames)
    {
        if (PeersChanged != null) await PeersChanged(hostNames);
    }
    protected virtual Task OnKicked(string reason) => Task.CompletedTask;
    protected virtual Task OnAuthenticated() => Task.CompletedTask;
    protected virtual Task OnDisconnected() => Task.CompletedTask;
    protected virtual void ConfigureHubUrl(HttpConnectionOptions options) => throw new NotSupportedException("Legacy relay disabled");
    protected override Task Execute(CancellationToken cancel)
    {
        // Keep world state construction available to derived routing classes, but never authenticate by hostname.
        _ = peerState;
        log.LogWarning("Legacy relay disabled; approved SecurePeerSession required");
        return Task.CompletedTask;
    }
}
