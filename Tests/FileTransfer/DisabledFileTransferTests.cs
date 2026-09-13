// Modified 2026-09-13 for the offline safety review fork; GPL-2.0. See MODIFICATIONS.md.
using Hydra.FileTransfer;
using Hydra.Relay;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Setup;

namespace Tests.FileTransfer;

[TestFixture]
public class DisabledFileTransferTests
{
    private sealed class ForbiddenDestination : IDropTargetResolver
    {
        public void MoveToDestination(string tempDir, string destDir) => throw new AssertionException("No disk writes allowed");
        public string? GetPasteDirectory() => throw new AssertionException("Disabled transfers must not inspect the destination");
    }

    [Test]
    public async Task DisabledService_RejectsAllEntryPointsWithoutReadingPathsOrCreatingFiles()
    {
        var dialog = new FakeFileTransferDialog();
        var sender = new FakeRelay();
        using var service = new FileTransferService(dialog, new ForbiddenDestination(), NullLogger<FileTransferService>.Instance);
        // Invalid/nonexistent paths must never be inspected. All three coordinator roles are tested.
        service.SetCopyBuffer("local", ["/must-not-be-read"]);
        service.HandleSelectionResponse("peer", "invalid json"u8.ToArray());
        service.InitiateSend(["/must-not-be-read"], "peer", sender);
        await service.ExecuteStreamRequest(["/must-not-be-read"], "peer", sender);
        foreach (var (source, target) in new[] { ("local", "peer"), ("peer", "local"), ("peer", "other-peer") })
            Assert.That(service.InitiatePaste(new(source, ["/must-not-be-read"]), target, "local", sender), Is.False);
        foreach (var kind in Enum.GetValues<MessageKind>().Where(k => k.ToString().StartsWith("File", StringComparison.Ordinal)))
            await service.OnMessageAsync("peer", kind, "invalid json"u8.ToArray(), sender);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.GetCopyBuffer(), Is.Null);
            Assert.That(service.FileTransferOngoing, Is.False);
            Assert.That(sender.Sent, Is.Empty);
            Assert.That(dialog.LastState, Is.EqualTo("none"));
        }
    }
}
