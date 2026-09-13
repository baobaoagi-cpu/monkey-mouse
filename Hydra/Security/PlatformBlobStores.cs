// Modified 2026-09-13; GPL-2.0. Platform adapters compile-only in this review; never invoked by tests.
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Hydra.Security;

[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiBlobStore : IProtectedBlobStore
{
    private readonly string _path;
    public WindowsDpapiBlobStore(string? isolatedTestId = null)
    {
        if (isolatedTestId != null && !Guid.TryParseExact(isolatedTestId,"N",out _)) throw new ArgumentException("Invalid self-test ID");
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MonkeyMouse","review");
        _path=Path.Combine(isolatedTestId == null ? root : Path.Combine(root,"self-test",isolatedTestId),"vault.dpapi");
    }
    public IDisposable AcquireLease()=>VaultLease.Acquire(_path+".lock");
    internal void DeleteIsolatedTestItem(string id)
    {
        if(!Guid.TryParseExact(id,"N",out _) || !Path.GetDirectoryName(_path)!.EndsWith(id,StringComparison.Ordinal)) throw new InvalidOperationException("Not a self-test store");
        if(File.Exists(_path))File.Delete(_path);
    }
    [StructLayout(LayoutKind.Sequential)] private struct Blob { public int Length; public IntPtr Data; }
    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref Blob input, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out Blob output);
    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out Blob output);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
    private static byte[] Transform(byte[] input, bool protect)
    {
        if (input.Length > 128 * 1024) throw new CryptographicException("Protected blob too large");
        var pin = GCHandle.Alloc(input, GCHandleType.Pinned);
        var source = new Blob { Length = input.Length, Data = pin.AddrOfPinnedObject() };
        Blob result = default;
        try
        {
            // UI_FORBIDDEN; deliberately omit LOCAL_MACHINE so protection is current-user bound.
            var ok = protect ? CryptProtectData(ref source, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out result)
                : CryptUnprotectData(ref source, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out result);
            if (!ok) throw new CryptographicException(Marshal.GetLastWin32Error());
            var bytes = new byte[result.Length]; Marshal.Copy(result.Data, bytes, 0, bytes.Length); return bytes;
        }
        finally
        {
            if (result.Data != IntPtr.Zero)
            {
                for (var i = 0; i < result.Length; i++) Marshal.WriteByte(result.Data, i, 0);
                LocalFree(result.Data);
            }
            pin.Free();
        }
    }
    public byte[]? Read()
    {
        if (!File.Exists(_path)) return null;
        if (new FileInfo(_path).Length > 128 * 1024) throw new CryptographicException("Protected blob too large");
        return Transform(File.ReadAllBytes(_path), false);
    }
    public void Write(byte[] data)
    {
        var encrypted = Transform(data, true);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { file.Write(encrypted); file.Flush(true); }
            File.Move(temp, _path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); CryptographicOperations.ZeroMemory(encrypted); }
    }
}

[SupportedOSPlatform("macos")]
public sealed class MacKeychainBlobStore : IProtectedBlobStore
{
    private const string Security = "/System/Library/Frameworks/Security.framework/Security";
    private const string Core = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    // Only our dedicated item. No shell commands, keychain-wide enumeration, TCC edits, or ACL weakening.
    private const string Service = "MonkeyMouse.local-review.device-vault.v1";
    private readonly string _service;
    private readonly string _lockPath;
    public MacKeychainBlobStore(string? isolatedTestId = null)
    {
        if(isolatedTestId != null && !Guid.TryParseExact(isolatedTestId,"N",out _))throw new ArgumentException("Invalid self-test ID");
        _service=isolatedTestId==null?Service:Service+".self-test."+isolatedTestId;
        _lockPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MonkeyMouse","review",isolatedTestId==null?"vault.lock":"self-test-"+isolatedTestId+".lock");
    }
    public IDisposable AcquireLease()=>VaultLease.Acquire(_lockPath);
    [DllImport(Security)] private static extern int SecItemDelete(IntPtr query);
    internal void DeleteIsolatedTestItem(string id)
    {
        if(_service!=Service+".self-test."+id || !Guid.TryParseExact(id,"N",out _))throw new InvalidOperationException("Not a self-test store");
        using var q=BaseQuery();var status=SecItemDelete(q.Value);
        if(status!=0 && status!=-25300)throw new CryptographicException($"Self-test cleanup failed ({status})");
    }
    [DllImport(Security)] private static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);
    [DllImport(Security)] private static extern int SecItemAdd(IntPtr attributes, IntPtr result);
    [DllImport(Security)] private static extern int SecItemUpdate(IntPtr query, IntPtr attributes);
    [DllImport(Core)] private static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator, nint capacity, IntPtr keys, IntPtr values);
    [DllImport(Core)] private static extern void CFDictionarySetValue(IntPtr dictionary, IntPtr key, IntPtr value);
    [DllImport(Core)] private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, uint encoding);
    [DllImport(Core)] private static extern IntPtr CFDataCreate(IntPtr allocator, byte[] bytes, nint length);
    [DllImport(Core)] private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(Core)] private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(Core)] private static extern void CFRelease(IntPtr value);
    private sealed class Query : IDisposable
    {
        private readonly IntPtr _security = NativeLibrary.Load(Security);
        private readonly IntPtr _core = NativeLibrary.Load(Core);
        private readonly List<IntPtr> _owned = [];
        public IntPtr Value { get; }
        public Query()
        {
            Value = CFDictionaryCreateMutable(IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero);
            if (Value == IntPtr.Zero) throw new CryptographicException("Keychain allocation failed");
        }
        private IntPtr Symbol(string name) => Marshal.ReadIntPtr(NativeLibrary.GetExport(name.StartsWith("kCF", StringComparison.Ordinal) ? _core : _security, name));
        public void Constant(string key, string value) => CFDictionarySetValue(Value, Symbol(key), Symbol(value));
        public void Text(string key, string value)
        {
            var item = CFStringCreateWithCString(IntPtr.Zero, value, 0x08000100); _owned.Add(item);
            CFDictionarySetValue(Value, Symbol(key), item);
        }
        public void Data(byte[] bytes)
        {
            var item = CFDataCreate(IntPtr.Zero, bytes, bytes.Length); _owned.Add(item);
            CFDictionarySetValue(Value, Symbol("kSecValueData"), item);
        }
        public void Dispose() { CFRelease(Value); foreach (var p in _owned) CFRelease(p); NativeLibrary.Free(_security); NativeLibrary.Free(_core); }
    }
    private Query BaseQuery()
    {
        var q = new Query();
        q.Constant("kSecClass", "kSecClassGenericPassword");
        q.Text("kSecAttrService", _service); q.Text("kSecAttrAccount", "local-device");
        q.Constant("kSecUseDataProtectionKeychain", "kCFBooleanTrue");
        q.Constant("kSecAttrSynchronizable", "kCFBooleanFalse");
        q.Constant("kSecUseAuthenticationUI", "kSecUseAuthenticationUIFail");
        return q;
    }
    public byte[]? Read()
    {
        using var q = BaseQuery(); q.Constant("kSecReturnData", "kCFBooleanTrue");
        var status = SecItemCopyMatching(q.Value, out var data);
        if (status == -25300) return null;
        if (status != 0) throw new CryptographicException($"Keychain read failed ({status}); no plaintext fallback");
        try
        {
            var length = CFDataGetLength(data);
            if (length < 1 || length > 64 * 1024) throw new CryptographicException("Invalid keychain item length");
            var bytes = new byte[(int)length]; Marshal.Copy(CFDataGetBytePtr(data), bytes, 0, bytes.Length); return bytes;
        }
        finally { CFRelease(data); }
    }
    public void Write(byte[] data)
    {
        if (data.Length > 64 * 1024) throw new CryptographicException("Vault too large");
        using var q = BaseQuery(); using var update = new Query(); update.Data(data);
        var status = SecItemUpdate(q.Value, update.Value);
        if (status == -25300)
        {
            q.Data(data); q.Constant("kSecAttrAccessible", "kSecAttrAccessibleWhenUnlockedThisDeviceOnly");
            status = SecItemAdd(q.Value, IntPtr.Zero);
        }
        if (status != 0) throw new CryptographicException($"Keychain write failed ({status}); no plaintext fallback");
    }
}
