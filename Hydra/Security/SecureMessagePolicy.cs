// Modified 2026-09-13; GPL-2.0. See MODIFICATIONS.md.
using System.Text.Json;
using Hydra.Relay;

namespace Hydra.Security;

internal static class SecureMessagePolicy
{
    internal const int MaxFrame = 1024 * 1024;
    internal static bool Allows(PeerGrant grant, MessageKind kind, bool sending) => kind switch
    {
        MessageKind.KeyEvent or MessageKind.MouseMove or MessageKind.MouseMoveDelta or MessageKind.MouseButton or
        MessageKind.MouseScroll or MessageKind.EnterScreen or MessageKind.LeaveScreen or MessageKind.MasterConfig => sending ? grant.SendInput : grant.ReceiveInput,
        MessageKind.ScreenInfo => sending ? grant.ReceiveInput : grant.SendInput,
        MessageKind.ClipboardPush or MessageKind.ClipboardPull or MessageKind.ClipboardPullResponse or
        MessageKind.ClipboardHash or MessageKind.ClipboardPullRequest => grant.Clipboard,
        _ => false // files, remote logs, OSD, power, unknown/future messages: fail closed
    };
    internal static byte[]? Filter(byte[] payload, bool sending)
    {
        if (payload.Length < 2 || payload.Length > MaxFrame) return null;
        var kind = (MessageKind)payload[0];
        if (kind is not (MessageKind.ClipboardPush or MessageKind.ClipboardPullResponse)) return payload;
        try
        {
            using var doc = JsonDocument.Parse(payload.AsMemory(1));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            string? text = null, primary = null; bool? unchanged = null;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!seen.Add(prop.Name)) return null;
                switch (prop.Name.ToLowerInvariant())
                {
                    case "text": if (prop.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) return null; text = prop.Value.GetString(); break;
                    case "primarytext": if (prop.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) return null; primary = prop.Value.GetString(); break;
                    case "unchanged":
                        if (kind != MessageKind.ClipboardPullResponse || prop.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null)) return null;
                        unchanged = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetBoolean(); break;
                    case "imagepng": case "html": case "rtf":
                        if (!sending && prop.Value.ValueKind != JsonValueKind.Null) return null;
                        break;
                    default: return null; // file lists, URI-format fields and unknown extensions cannot ride along
                }
            }
            if (System.Text.Encoding.UTF8.GetByteCount(text ?? "") + System.Text.Encoding.UTF8.GetByteCount(primary ?? "") > Hydra.Platform.ClipboardUtils.MaxClipboardBytes) return null;
            return kind == MessageKind.ClipboardPush
                ? MessageSerializer.Encode(kind, new ClipboardPushMessage(text ?? "", primary))
                : MessageSerializer.Encode(kind, new ClipboardPullResponseMessage(text, primary, Unchanged: unchanged));
        }
        catch (JsonException) { return null; }
    }
}
