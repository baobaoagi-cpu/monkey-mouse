// Modified 2026-09-13 for Monkey Mouse; GPL-2.0. See MODIFICATIONS.md.
namespace Hydra.Security;

// Lifetime exclusive writer ownership. Never unlink the lock: unlinking a locked inode permits
// a new process to lock a different inode while the old owner still has live trust state.
public sealed class VaultLease : IDisposable
{
    private readonly FileStream _file;
    private VaultLease(FileStream file) => _file = file;
    public static VaultLease Acquire(string lockPath)
    {
        var parent=Path.GetDirectoryName(Path.GetFullPath(lockPath))!;
        Directory.CreateDirectory(parent);
        var file=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        try
        {
            if(!OperatingSystem.IsWindows()) File.SetUnixFileMode(lockPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
            return new VaultLease(file);
        }
        catch {file.Dispose();throw;}
    }
    public void Dispose()=>_file.Dispose();
}
