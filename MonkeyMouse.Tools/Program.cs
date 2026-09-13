// Monkey Mouse — GPL-2.0 derivative of Hydra. Modified 2026-09-13. See LICENSE/MODIFICATIONS.md.
using System.Text.Json;
using System.Text.Json.Serialization;
using Hydra.Config;
using Hydra.Review;
using Hydra.Security;

var json=new JsonSerializerOptions{WriteIndented=true,PropertyNameCaseInsensitive=true,Converters={new JsonStringEnumConverter()}};
string? Option(string name){var i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
void RequireWriteConsent(){if(!args.Contains("--allow-store-write"))throw new InvalidOperationException("This command writes Monkey Mouse protected storage/lock files. Review the action, then explicitly add --allow-store-write. It never grants OS permissions or starts input control.");}
IProtectedBlobStore NativeStore()
{
    if(OperatingSystem.IsMacOS())return new MacKeychainBlobStore();
    if(OperatingSystem.IsWindows())return new WindowsDpapiBlobStore();
    throw new PlatformNotSupportedException();
}
try
{
    switch(args.FirstOrDefault()??"help")
    {
        case "help":case "--help":
            Console.WriteLine("""
Monkey Mouse — Windows/Mac 四螢幕鍵鼠共享工具的安全審查命令
Derived from Hydra; GPL-2.0; no warranty. No input controller or network listener is started here.

Read-only commands:
  about
  displays --host Mac|ASUS
  config-check --measurements <json>

Protected storage commands (explicit --allow-store-write required):
  storage-self-test                  Dedicated temporary native item; cleans up only that item
  identity-init                     Create this device's independent key; never replace existing identity
  identity-show                     Show public fingerprint; holds exclusive local lock
  invite-export                     Output PUBLIC invitation JSON only
  peers                             Show approved peers and capabilities
  pair --invite <json> --alias <name> --role send-input|receive-input|clipboard-only
  revoke --peer <full-sha256>

Pair/revoke ask for confirmation in this terminal; never paste passwords into chat.
run/input/install/service are intentionally unavailable until native-store and hardware validation.
""");break;
        case "about":Console.WriteLine("Monkey Mouse\nHydra-derived GPL-2.0 review tooling. Original Hydra copyright (c) 2026 Cathedral; modifications 2026. No warranty. Not a notarized/production release.");break;
        case "displays":Console.WriteLine(JsonSerializer.Serialize(DisplayInventory.Read(Option("--host")??(OperatingSystem.IsMacOS()?"Mac":"ASUS")),json));break;
        case "config-check":
            var path=Option("--measurements")??throw new ArgumentException("Missing --measurements");
            var measurement=JsonSerializer.Deserialize<FourScreenMeasurements>(File.ReadAllText(path),json)??throw new ArgumentException("Invalid measurements");
            Console.WriteLine(JsonSerializer.Serialize(FourScreenProfileBuilder.Build(measurement),json));break;
        case "storage-self-test":RequireWriteConsent();Console.WriteLine(NativeStoreSelfTest.Run());break;
        case "identity-init":RequireWriteConsent();using(var vault=DeviceVault.Create(NativeStore()))Console.WriteLine($"Monkey Mouse Device ID: {vault.DeviceId}");break;
        case "identity-show":RequireWriteConsent();using(var vault=DeviceVault.Open(NativeStore()))Console.WriteLine($"Monkey Mouse Device ID: {vault.DeviceId}");break;
        case "invite-export":RequireWriteConsent();using(var vault=DeviceVault.Open(NativeStore()))Console.WriteLine(JsonSerializer.Serialize(vault.Invitation(),json));break;
        case "peers":RequireWriteConsent();using(var vault=DeviceVault.Open(NativeStore()))Console.WriteLine(JsonSerializer.Serialize(vault.Peers,json));break;
        case "pair":
            RequireWriteConsent();
            var invitationPath=Option("--invite")??throw new ArgumentException("Missing --invite");
            if(new FileInfo(invitationPath).Length>16384)throw new ArgumentException("Invitation exceeds bound");
            var invitation=JsonSerializer.Deserialize<PairingInvitation>(File.ReadAllText(invitationPath),json)??throw new ArgumentException("Invalid invitation");
            using(var vault=DeviceVault.Open(NativeStore()))PairingWorkflow.Pair(vault,invitation,Option("--alias")??throw new ArgumentException("Missing --alias"),Option("--role")??throw new ArgumentException("Missing --role"),Console.In,Console.Out);break;
        case "revoke":RequireWriteConsent();using(var vault=DeviceVault.Open(NativeStore()))PairingWorkflow.Revoke(vault,Option("--peer")??throw new ArgumentException("Missing --peer"),Console.In,Console.Out);break;
        default:throw new ArgumentException("Unknown/unavailable command. Run --help. No input or network test was started.");
    }
    return 0;
}
catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or IOException or System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"Monkey Mouse: {ex.Message}");return 2;
}
