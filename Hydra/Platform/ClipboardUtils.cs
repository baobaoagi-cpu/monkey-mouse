// Modified 2026-09-13 for four-screen security review v2; GPL-2.0. See MODIFICATIONS.md.
using System.IO.Hashing;
using System.Text;
using ByteSizeLib;
using Microsoft.Extensions.Logging;

namespace Hydra.Platform;

internal struct ClipboardEchoFilter
{
    private string? _lastText;
    private ulong? _lastImageHash;
    private ulong? _lastHtmlHash;
    private ulong? _lastRtfHash;

    public void TrackText(string text) => _lastText = text;
    public void TrackImage(byte[] png) => _lastImageHash = ClipboardUtils.QuickHash(png);
    public void TrackHtml(string html) => _lastHtmlHash = ClipboardUtils.QuickHash(Encoding.UTF8.GetBytes(html));
    public void TrackRtf(byte[] rtf) => _lastRtfHash = ClipboardUtils.QuickHash(rtf);
    public readonly string? FilterText(string? text) => text == _lastText ? null : text;
    public readonly bool IsDuplicateImage(byte[] png) => _lastImageHash.HasValue && ClipboardUtils.QuickHash(png) == _lastImageHash.Value;
    public readonly string? FilterHtml(string? html) => html != null && _lastHtmlHash.HasValue && ClipboardUtils.QuickHash(Encoding.UTF8.GetBytes(html)) == _lastHtmlHash.Value ? null : html;
    public readonly byte[]? FilterRtf(byte[]? rtf) => rtf != null && _lastRtfHash.HasValue && ClipboardUtils.QuickHash(rtf) == _lastRtfHash.Value ? null : rtf;
}

public static class ClipboardUtils
{
    public static readonly long MaxClipboardBytes = 128 * 1024;

    // Only plain text crosses the application boundary. No rich-format platform getters are called.
    public static ClipboardSnapshot ValidateFields(string? text, string? primaryText, byte[]? image, string? html, byte[]? rtf, ILogger log, string context, string host) =>
        TrimToFit(text, primaryText, null, null, null, log, context);

    public static ClipboardSnapshot ReadWithFallback(IClipboardSync sync, ClipboardSnapshot? fallback, ILogger log, string context)
    {
        // Without a platform change-token, a null GetText cannot distinguish echo suppression from
        // a new non-text clipboard item. Do not resurrect stale text from fallback in that case.
        return TrimToFit(sync.GetText(), sync.GetPrimaryText(), null, null, null, log, context);
    }

    public static ClipboardSnapshot TrimToFit(string? text, string? primaryText, byte[]? image, string? html, byte[]? rtf, ILogger log, string context)
    {
        image = null; html = null; rtf = null;
        long textBytes = text != null ? Encoding.UTF8.GetByteCount(text) : 0;
        long primaryBytes = primaryText != null ? Encoding.UTF8.GetByteCount(primaryText) : 0;
        long imageBytes = image?.Length ?? 0;
        long htmlBytes = html != null ? Encoding.UTF8.GetByteCount(html) : 0;
        long rtfBytes = rtf?.Length ?? 0;
        long Total() => textBytes + primaryBytes + imageBytes + htmlBytes + rtfBytes;

        if (Total() > MaxClipboardBytes)
        {
            log.LogWarning("Clipboard {Context} too large ({Total} bytes), dropping image", context, Total());
            image = null; imageBytes = 0;
        }
        if (Total() > MaxClipboardBytes)
        {
            log.LogWarning("Clipboard {Context} still too large ({Total} bytes), dropping html", context, Total());
            html = null; htmlBytes = 0;
        }
        if (Total() > MaxClipboardBytes)
        {
            log.LogWarning("Clipboard {Context} still too large ({Total} bytes), dropping rtf", context, Total());
            rtf = null; rtfBytes = 0;
        }
        if (Total() > MaxClipboardBytes)
        {
            log.LogWarning("Clipboard {Context} still too large ({Total} bytes), dropping primary text", context, Total());
            primaryText = null; primaryBytes = 0;
        }
        if (Total() > MaxClipboardBytes)
        {
            log.LogWarning("Clipboard {Context} still too large ({Total} bytes), dropping text", context, Total());
            text = null;
        }
        return new ClipboardSnapshot(text, primaryText, image, html, rtf);
    }

    public static ulong QuickHash(byte[] data)
    {
        // two hashes with different inputs combined into 64-bit to reduce collision probability
        var hc1 = new HashCode();
        hc1.AddBytes(data);
        var hc2 = new HashCode();
        hc2.Add(data.Length); // prefix with length to differentiate from hc1
        hc2.AddBytes(data);
        return ((ulong)(uint)hc1.ToHashCode() << 32) | (uint)hc2.ToHashCode();
    }

    // xxhash64 of all 3 clipboard fields; used to avoid redundant syncs between master and slave
    public static ulong ClipboardHash(ClipboardSnapshot snap)
    {
        var hash = new XxHash64();
        Append(hash, snap.Text != null ? Encoding.UTF8.GetBytes(snap.Text) : []);
        Append(hash, snap.PrimaryText != null ? Encoding.UTF8.GetBytes(snap.PrimaryText) : []);
        Append(hash, snap.ImagePng ?? []);
        Append(hash, snap.Html != null ? Encoding.UTF8.GetBytes(snap.Html) : []);
        Append(hash, snap.Rtf ?? []);
        return BitConverter.ToUInt64(hash.GetCurrentHash().AsSpan());

        static void Append(XxHash64 h, byte[] data)
        {
            h.Append(BitConverter.GetBytes(data.Length));
            h.Append(data);
        }
    }
}
