// Modified 2026-09-13 for Monkey Mouse; GPL-2.0. Explicit native-store write test; never run automatically.
using System.Security.Cryptography;
using Hydra.Security;
namespace Hydra.Review;
public static class NativeStoreSelfTest
{
    public static string Run()
    {
        var id=Guid.NewGuid().ToString("N");
        IProtectedBlobStore store;Action cleanup;
        if(OperatingSystem.IsMacOS())
        {
            var mac=new MacKeychainBlobStore(id);store=mac;
            cleanup=()=> { if(OperatingSystem.IsMacOS())mac.DeleteIsolatedTestItem(id); };
        }
        else if(OperatingSystem.IsWindows())
        {
            var win=new WindowsDpapiBlobStore(id);store=win;
            cleanup=()=> { if(OperatingSystem.IsWindows())win.DeleteIsolatedTestItem(id); };
        }
        else throw new PlatformNotSupportedException();
        using var lease=store.AcquireLease();
        var bytes=RandomNumberGenerator.GetBytes(32);
        try
        {
            if(store.Read()!=null)throw new InvalidOperationException("Self-test identifier unexpectedly exists");
            store.Write(bytes);var read=store.Read()??throw new CryptographicException("Readback missing");
            try {if(!CryptographicOperations.FixedTimeEquals(bytes,read))throw new CryptographicException("Readback mismatch");}
            finally {CryptographicOperations.ZeroMemory(read);}
            // Persist a second value: exercise update as well as initial add.
            bytes[0]^=1;store.Write(bytes);read=store.Read()??throw new CryptographicException("Update missing");
            try {if(!CryptographicOperations.FixedTimeEquals(bytes,read))throw new CryptographicException("Update mismatch");}
            finally {CryptographicOperations.ZeroMemory(read);}
        }
        finally {CryptographicOperations.ZeroMemory(bytes);cleanup();}
        if(store.Read()!=null)throw new CryptographicException("Self-test cleanup did not remove item");
        return "Native protected-store create/read/update/delete passed; no device identity or peer trust was created. Empty lock file remains.";
    }
}
