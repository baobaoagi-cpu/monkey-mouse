// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
using Hydra.Config;

namespace Hydra.Relay;

// Local policy applies in BOTH directions, before dispatch/queueing. It is not peer authentication.
internal static class MessagePolicy
{
    internal static bool IsFileMessage(MessageKind kind) => kind is
        MessageKind.FileTransferRequest or MessageKind.FileTransferStart or
        MessageKind.FileTransferChunk or MessageKind.FileTransferDone or
        MessageKind.FileTransferAbort or MessageKind.FileTransferAccepted or
        MessageKind.FileSelectionQuery or MessageKind.FileSelectionResponse or
        MessageKind.FileStreamRequest or MessageKind.FileTransferBusy;

    internal static bool Allows(IHydraProfile profile, MessageKind kind) => kind switch
    {
        _ when IsFileMessage(kind) => profile.EnableFileTransfer,
        MessageKind.ScreensaverSync or MessageKind.ActivityPing => profile.SyncScreensaver,
        MessageKind.LockScreen => profile.AllowRemoteLock,
        _ => true
    };
}
