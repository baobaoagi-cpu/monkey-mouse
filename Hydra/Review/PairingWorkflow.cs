// Modified 2026-09-13 for Monkey Mouse; GPL-2.0. See MODIFICATIONS.md.
using Hydra.Security;
namespace Hydra.Review;

public static class PairingWorkflow
{
    public const string ProductName="Monkey Mouse";
    public static bool Pair(DeviceVault local,PairingInvitation invitation,string alias,string role,TextReader input,TextWriter output)
    {
        var permissions=role switch
        {
            "send-input"=>(Receive:false,Send:true),
            "receive-input"=>(Receive:true,Send:false),
            "clipboard-only"=>(Receive:false,Send:false),
            _=>throw new ArgumentException("Role must be send-input, receive-input or clipboard-only")
        };
        output.WriteLine("Monkey Mouse — 雙方核准配對（此操作不會開始控制滑鼠）");
        output.WriteLine($"本機 Device ID：{local.DeviceId}");
        output.WriteLine($"對方的本機別名：{alias}；允許送出輸入：{permissions.Send}；允許接收輸入：{permissions.Receive}；雙向純文字：是");
        output.WriteLine("請看『另一台電腦』的 identity-show，輸入其完整 64 位 SHA-256 指紋；不是密碼。不要從邀請檔或聊天複製指紋。");
        var fingerprint=NormalizeFingerprint(input.ReadLine());
        var pending=local.BeginApproval(invitation,fingerprint,alias,permissions.Receive,permissions.Send,true);
        output.WriteLine("指紋吻合。檔案、鎖定、喚醒與螢幕保護同步不開放。請於兩分鐘內輸入 PAIR 確認，其他輸入取消：");
        if(input.ReadLine()!="PAIR") {output.WriteLine("已取消，沒有新增信任。");return false;}
        local.Confirm(pending.Token);
        output.WriteLine("本機核准已儲存。另一台仍須獨立核對並核准，之後才可能建立加密會話。");
        return true;
    }
    public static bool Revoke(DeviceVault local,string deviceId,TextReader input,TextWriter output)
    {
        var peer=local.Find(NormalizeFingerprint(deviceId))??throw new InvalidOperationException("Peer not found");
        output.WriteLine($"將移除 {peer.Alias} 的核准：{peer.DeviceId}。輸入 REVOKE 確認：");
        if(input.ReadLine()!="REVOKE")return false;
        local.Revoke(peer.DeviceId);output.WriteLine("核准已撤銷。");return true;
    }
    public static string NormalizeFingerprint(string? text)
    {
        var value=(text??"").Replace(" ","").Replace(":","").ToUpperInvariant();
        if(value.Length!=64||!value.All(Uri.IsHexDigit))throw new ArgumentException("A complete SHA-256 fingerprint is required");
        return value;
    }
}
