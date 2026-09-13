// Modified 2026-09-13; GPL-2.0. See MODIFICATIONS.md.
using System.Buffers.Binary;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Hydra.Relay;

namespace Hydra.Security;

// Transport-independent: caller supplies a duplex stream. Tests supply memory pipes, never sockets.
// No cryptography is implemented here: record confidentiality, authentication and replay handling use SslStream.
public sealed class SecurePeerSession : IAsyncDisposable
{
    private static readonly SslApplicationProtocol Protocol = new("monkey-mouse-review-v1");
    private readonly DeviceVault _vault;
    private readonly SslStream _tls;
    private readonly SemaphoreSlim _write = new(1, 1);
    private readonly SemaphoreSlim _read = new(1, 1);
    private ulong _sent, _received;
    private int _closed;
    public string PeerId { get; }
    public string Alias { get; }
    public SslProtocols NegotiatedProtocol => _tls.SslProtocol;
    public bool IsOpen => Volatile.Read(ref _closed) == 0 && _vault.Find(PeerId) is not null;
    public event Action? Closed;
    private SecurePeerSession(DeviceVault vault, string peerId, string alias, SslStream tls)
    {
        _vault = vault; PeerId = peerId; Alias = alias; _tls = tls;
        _vault.Revoked += OnRevoked;
    }
    public static async Task<SecurePeerSession> Authenticate(Stream stream, DeviceVault local, string expectedPeerId,
        bool server, CancellationToken cancellationToken = default)
    {
        var grant = local.Find(expectedPeerId) ?? throw new AuthenticationException("Peer has not been approved locally");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var tls = new SslStream(stream, false, (_, cert, _, _) =>
        {
            if (cert is null || local.Find(expectedPeerId) is null || DeviceVault.Id(cert) != expectedPeerId) return false;
            using var parsed = X509CertificateLoader.LoadCertificate(cert.GetRawCertData());
            var now = DateTime.UtcNow;
            // Exact previously approved cert pin is our trust anchor; public CA/hostname chain is not used.
            // SslStream still verifies TLS CertificateVerify (private-key possession).
            return parsed.NotBefore.ToUniversalTime() <= now && parsed.NotAfter.ToUniversalTime() > now;
        });
        SecurePeerSession? session = null;
        try
        {
            if (server)
                await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = local.Certificate, ClientCertificateRequired = true,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    AllowRenegotiation = false, AllowTlsResume = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    ApplicationProtocols = [Protocol]
                }, timeout.Token);
            else
                await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    TargetHost = "hydra-pinned-peer", ClientCertificates = [local.Certificate],
                    LocalCertificateSelectionCallback = (_, _, _, _, _) => local.Certificate,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    AllowRenegotiation = false, AllowTlsResume = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    ApplicationProtocols = [Protocol]
                }, timeout.Token);
            if (!tls.IsMutuallyAuthenticated || tls.RemoteCertificate is null || DeviceVault.Id(tls.RemoteCertificate) != expectedPeerId ||
                tls.NegotiatedApplicationProtocol != Protocol || local.Find(expectedPeerId) is null)
                throw new AuthenticationException("Mutual authentication/approval required");
            // macOS SecureTransport may only expose TLS 1.2. Require ECDHE+AEAD there; never weak fallback.
            if (tls.SslProtocol == SslProtocols.Tls12 && tls.NegotiatedCipherSuite is not
                (TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 or TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384))
                throw new AuthenticationException("TLS 1.2 requires ECDHE/ECDSA/AES-GCM");
            if (tls.SslProtocol is not (SslProtocols.Tls12 or SslProtocols.Tls13)) throw new AuthenticationException("Unsupported TLS version");
            // Both ends must finish their own validation before a session is returned to the application.
            // This is a fixed application readiness/version record inside TLS, not a homegrown handshake.
            var hello = Encoding.ASCII.GetBytes("HYDRA1" + local.DeviceId + expectedPeerId);
            await tls.WriteAsync(hello, timeout.Token); await tls.FlushAsync(timeout.Token);
            var remoteHello = new byte[hello.Length]; await tls.ReadExactlyAsync(remoteHello, timeout.Token);
            if (Encoding.ASCII.GetString(remoteHello) != "HYDRA1" + expectedPeerId + local.DeviceId)
                throw new AuthenticationException("Peer readiness mismatch");
            session = new SecurePeerSession(local, expectedPeerId, grant.Alias, tls);
            if (!session.IsOpen) throw new AuthenticationException("Approval revoked during handshake");
            return session;
        }
        catch { if (session is not null) session.Close(); else await tls.DisposeAsync(); throw; }
    }
    public async Task<bool> Send(byte[] payload, CancellationToken cancel = default)
    {
        var clean = SecureMessagePolicy.Filter(payload, sending: true);
        if (clean is null) return false;
        await _write.WaitAsync(cancel);
        try
        {
            var grant = RequireGrant();
            if (!SecureMessagePolicy.Allows(grant, (MessageKind)clean[0], sending: true)) return false;
            if (_sent == ulong.MaxValue) throw new IOException("Session sequence exhausted");
            var header = new byte[12]; BinaryPrimitives.WriteInt32BigEndian(header, clean.Length);
            BinaryPrimitives.WriteUInt64BigEndian(header.AsSpan(4), _sent++);
            await _tls.WriteAsync(header, cancel); await _tls.WriteAsync(clean, cancel); await _tls.FlushAsync(cancel);
            return true;
        }
        catch { Close(); throw; }
        finally { _write.Release(); }
    }
    public async Task<byte[]?> Receive(CancellationToken cancel = default)
    {
        await _read.WaitAsync(cancel);
        try
        {
            RequireGrant();
            var header = new byte[12]; await _tls.ReadExactlyAsync(header, cancel);
            var length = BinaryPrimitives.ReadInt32BigEndian(header);
            var sequence = BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(4));
            if (length < 2 || length > SecureMessagePolicy.MaxFrame || sequence != _received || _received == ulong.MaxValue)
                throw new IOException("Invalid frame bound or sequence/replay");
            _received++;
            var bytes = new byte[length]; await _tls.ReadExactlyAsync(bytes, cancel);
            var grant = RequireGrant();
            if (!SecureMessagePolicy.Allows(grant, (MessageKind)bytes[0], sending: false)) return null;
            return SecureMessagePolicy.Filter(bytes, sending: false);
        }
        catch { Close(); throw; }
        finally { _read.Release(); }
    }
    private PeerGrant RequireGrant() => IsOpen ? _vault.Find(PeerId) ?? throw new AuthenticationException("Peer revoked")
        : throw new AuthenticationException("Session closed or revoked");
    private void OnRevoked(string id) { if (id == PeerId) Close(); }
    private void Close()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0) return;
        _vault.Revoked -= OnRevoked; _tls.Dispose(); Closed?.Invoke();
    }
    public ValueTask DisposeAsync() { Close(); return ValueTask.CompletedTask; }
}
