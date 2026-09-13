// Modified 2026-09-13; Monkey Mouse GPL-2.0. Test probe: only owns a scratch file lock; no product startup.
using Hydra.Security;
if(args.Length!=1) return 2;
try
{
    using var lease=VaultLease.Acquire(args[0]);
    Console.WriteLine("LEASED");Console.Out.Flush();
    Console.ReadLine();return 0;
}
catch(IOException){Console.WriteLine("BUSY");return 3;}
