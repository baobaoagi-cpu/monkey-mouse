// Modified 2026-09-13; GPL-2.0. Offline security review. See MODIFICATIONS.md.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Hydra.Security;

// Implementations MUST protect confidentiality/integrity at rest. No plaintext-file fallback.
public interface IProtectedBlobStore
{
    byte[]? Read();
    void Write(byte[] data);
    IDisposable AcquireLease();
}

public sealed record PeerGrant(string DeviceId, string Alias, bool ReceiveInput, bool SendInput, bool Clipboard);
public sealed record PairingInvitation(int Version, string CertificateDer);
public sealed record PendingPairing(string Token, string DeviceId, DateTimeOffset Expires);

public sealed class DeviceVault : IDisposable
{
    private sealed record State(int Version, string Pfx, List<PeerGrant> Peers);
    private readonly IProtectedBlobStore _store;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _gate = new();
    private readonly Dictionary<string, (PendingPairing Pending, PeerGrant Grant)> _pending = new();
    private State _state;
    private readonly IDisposable _lease;
    private int _disposed;
    internal X509Certificate2 Certificate { get; }
    public string DeviceId => Id(Certificate);
    public event Action<string>? Revoked;
    private DeviceVault(IProtectedBlobStore store, State state, Func<DateTimeOffset>? clock, IDisposable lease)
    {
        _store = store; _state = state; _clock = clock ?? (() => DateTimeOffset.UtcNow); _lease = lease;
        // macOS .NET does not support EphemeralKeySet; without PersistKeySet it uses a disposable
        // temporary keychain, not the login/system trust store. Certificate.Dispose releases it.
        var pfx = Convert.FromBase64String(state.Pfx);
        try { Certificate = X509CertificateLoader.LoadPkcs12(pfx, null, OperatingSystem.IsMacOS() ? X509KeyStorageFlags.Exportable : X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable); }
        finally { CryptographicOperations.ZeroMemory(pfx); }
        try
        {
        if (!Certificate.HasPrivateKey || state.Version != 1 || state.Peers.Count > 16 ||
            state.Peers.Select(p => p.DeviceId).Distinct().Count() != state.Peers.Count ||
            state.Peers.Select(p => p.Alias).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.Peers.Count)
            throw new CryptographicException("Invalid device vault");
        foreach (var peer in state.Peers) ValidateGrant(peer);
        }
        catch { Certificate.Dispose(); throw; }
    }
    public static DeviceVault Create(IProtectedBlobStore store, Func<DateTimeOffset>? clock = null)
    {
        var lease = store.AcquireLease();
        try
        {
        var existing = store.Read();
        if (existing is not null) { CryptographicOperations.ZeroMemory(existing); throw new InvalidOperationException("Identity already exists; never overwrite automatically"); }
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Monkey Mouse independent device", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        var now = (clock ?? (() => DateTimeOffset.UtcNow))();
        using var cert = request.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(1));
        var pfx = cert.Export(X509ContentType.Pkcs12);
        try
        {
            var state = new State(1, Convert.ToBase64String(pfx), []);
            Persist(store, state);
            return new DeviceVault(store, state, clock, lease);
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
        } catch { lease.Dispose(); throw; }
    }
    public static DeviceVault Open(IProtectedBlobStore store, Func<DateTimeOffset>? clock = null)
    {
        var lease = store.AcquireLease();
        try
        {
        var bytes = store.Read() ?? throw new InvalidOperationException("Identity missing: explicit creation required");
        try
        {
            if (bytes.Length > 64 * 1024) throw new CryptographicException("Vault exceeds bound");
            return new DeviceVault(store, JsonSerializer.Deserialize<State>(bytes) ?? throw new CryptographicException("Invalid vault"), clock, lease);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
        } catch { lease.Dispose(); throw; }
    }
    public PeerGrant[] Peers { get { lock (_gate) { ThrowIfDisposed(); return [.. _state.Peers]; } } }
    public PairingInvitation Invitation() => new(1, Convert.ToBase64String(Certificate.RawData));
    public PendingPairing BeginApproval(PairingInvitation invitation, string verifiedFingerprint, string alias,
        bool receiveInput, bool sendInput, bool clipboard)
    {
        // verifiedFingerprint is the FULL SHA-256 read/scanned from the OTHER device's local display.
        // It must not be copied from unauthenticated network discovery by a future UI.
        if (invitation.Version != 1 || invitation.CertificateDer.Length > 8192) throw new CryptographicException("Invalid invitation");
        using var peer = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(invitation.CertificateDer));
        var id = Id(peer);
        if (!string.Equals(id, verifiedFingerprint, StringComparison.OrdinalIgnoreCase) || id == DeviceId ||
            peer.NotAfter.ToUniversalTime() <= _clock().UtcDateTime || peer.NotBefore.ToUniversalTime() > _clock().UtcDateTime)
            throw new CryptographicException("Fingerprint or validity mismatch");
        var grant = new PeerGrant(id, alias, receiveInput, sendInput, clipboard); ValidateGrant(grant);
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var key in _pending.Where(p => p.Value.Pending.Expires <= _clock()).Select(p => p.Key).ToArray()) _pending.Remove(key);
            if (_pending.Count >= 4) throw new InvalidOperationException("Too many pending approvals");
            if (_state.Peers.Any(p => p.DeviceId == id || p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Already paired or alias in use; revoke explicitly first");
            var pending = new PendingPairing(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), id, _clock().AddMinutes(2));
            _pending.Add(pending.Token, (pending, grant)); return pending;
        }
    }
    public void Confirm(string token)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_pending.Remove(token, out var item) || item.Pending.Expires <= _clock()) throw new InvalidOperationException("Approval missing, used or expired");
            if (_state.Peers.Count >= 16 || _state.Peers.Any(p => p.DeviceId == item.Grant.DeviceId || p.Alias.Equals(item.Grant.Alias, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Duplicate or capacity exceeded");
            var next = _state with { Peers = [.. _state.Peers, item.Grant] };
            Persist(_store, next); _state = next; // publish trust only AFTER durable write succeeds
        }
    }
    public PeerGrant? Find(string deviceId) { lock (_gate) return _disposed == 0 ? _state.Peers.FirstOrDefault(p => p.DeviceId == deviceId) : null; }
    public PeerGrant? FindAlias(string alias) { lock (_gate) return _disposed == 0 ? _state.Peers.FirstOrDefault(p => p.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase)) : null; }
    public void Revoke(string deviceId)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var key in _pending.Where(p => p.Value.Grant.DeviceId == deviceId).Select(p => p.Key).ToArray()) _pending.Remove(key);
            var next = _state with { Peers = _state.Peers.Where(p => p.DeviceId != deviceId).ToList() };
            Persist(_store, next); _state = next;
        }
        Revoked?.Invoke(deviceId);
    }
    public static string Id(X509Certificate certificate) => Convert.ToHexString(SHA256.HashData(certificate.GetRawCertData()));
    private static void ValidateGrant(PeerGrant grant)
    {
        if (grant.DeviceId.Length != 64 || !grant.DeviceId.All(Uri.IsHexDigit) ||
            string.IsNullOrWhiteSpace(grant.Alias) || grant.Alias.Length > 64 || grant.Alias.Any(char.IsControl))
            throw new CryptographicException("Invalid peer identity/alias");
    }
    private static void Persist(IProtectedBlobStore store, State state)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state);
        try { if (bytes.Length > 64 * 1024) throw new CryptographicException("Vault exceeds bound"); store.Write(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private void ThrowIfDisposed() { if (_disposed != 0) throw new ObjectDisposedException(nameof(DeviceVault)); }
    public void Dispose()
    {
        PeerGrant[] peers;
        lock (_gate)
        {
            if (_disposed != 0) return;
            _disposed = 1;
            peers = [.. _state.Peers];
            _pending.Clear();
        }
        try
        {
            // Active sessions close before the exclusive writer lease can move to another process.
            foreach (var peer in peers)
                foreach (Action<string> handler in Revoked?.GetInvocationList() ?? [])
                    try { handler(peer.DeviceId); } catch { /* continue closing other sessions */ }
            Certificate.Dispose(); lock (_gate) _pending.Clear();
        }
        finally { _lease.Dispose(); }
    }
}
