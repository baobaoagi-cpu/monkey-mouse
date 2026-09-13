// Monkey Mouse, modified 2026-09-13; GPL-2.0. In-memory trust only.
using System.Security.Cryptography;
using Hydra.Review;
using Hydra.Security;
namespace Tests.Security;
[TestFixture]
public class PairingWorkflowTests
{
    [TestCase("send-input", false, true)]
    [TestCase("receive-input", true, false)]
    [TestCase("clipboard-only", false, false)]
    public void ExplicitFingerprintAndConfirmation_PersistOnlySelectedCapabilities(string role,bool receive,bool send)
    {
        var store=new MemoryProtectedStore();using var a=DeviceVault.Create(store);using var b=DeviceVault.Create(new MemoryProtectedStore());
        using var output=new StringWriter();
        Assert.That(PairingWorkflow.Pair(a,b.Invitation(),"peer",role,new StringReader(b.DeviceId.ToLowerInvariant()+"\nPAIR\n"),output),Is.True);
        var peer=a.Find(b.DeviceId)!;
        Assert.That((peer.ReceiveInput,peer.SendInput,peer.Clipboard),Is.EqualTo((receive,send,true)));
        Assert.That(b.Peers,Is.Empty,"One device's confirmation cannot approve the other device");
        a.Dispose();using var reopened=DeviceVault.Open(store);Assert.That(reopened.Find(b.DeviceId),Is.EqualTo(peer));
    }
    [TestCase("cancel")][TestCase("")][TestCase("pair")]
    public void CancellationOrEof_DoesNotPersistTrust(string confirmation)
    {
        var store=new MemoryProtectedStore();using var a=DeviceVault.Create(store);using var b=DeviceVault.Create(new MemoryProtectedStore());
        Assert.That(PairingWorkflow.Pair(a,b.Invitation(),"peer","send-input",new StringReader(b.DeviceId+"\n"+confirmation),TextWriter.Null),Is.False);
        a.Dispose();using var reopened=DeviceVault.Open(store);Assert.That(reopened.Peers,Is.Empty);
    }
    [Test]
    public void WrongFingerprint_RejectsEvenWithConfirmation()
    {
        using var a=DeviceVault.Create(new MemoryProtectedStore());using var b=DeviceVault.Create(new MemoryProtectedStore());
        Assert.Throws<CryptographicException>(()=>PairingWorkflow.Pair(a,b.Invitation(),"peer","receive-input",new StringReader(new string('0',64)+"\nPAIR\n"),TextWriter.Null));
        Assert.That(a.Peers,Is.Empty);
    }
    [Test]
    public void RevokeRequiresConfirmation_AndSurvivesReopen()
    {
        var store=new MemoryProtectedStore();using var a=DeviceVault.Create(store);using var b=DeviceVault.Create(new MemoryProtectedStore());
        SecureSessionTests.Approve(a,b,"peer");
        Assert.That(PairingWorkflow.Revoke(a,b.DeviceId,new StringReader("no"),TextWriter.Null),Is.False);
        Assert.That(a.Find(b.DeviceId),Is.Not.Null);
        Assert.That(PairingWorkflow.Revoke(a,b.DeviceId,new StringReader("REVOKE"),TextWriter.Null),Is.True);
        a.Dispose();using var reopened=DeviceVault.Open(store);Assert.That(reopened.Peers,Is.Empty);
    }
    [Test]
    public void SingleOwner_PreventsStaleWriterFromRestoringRevokedTrust()
    {
        var store=new MemoryProtectedStore();using var a=DeviceVault.Create(store);using var b=DeviceVault.Create(new MemoryProtectedStore());
        SecureSessionTests.Approve(a,b,"peer");
        Assert.Throws<IOException>(()=>DeviceVault.Open(store));
        a.Revoke(b.DeviceId);a.Dispose();using var next=DeviceVault.Open(store);
        Assert.That(next.Peers,Is.Empty);
        Assert.Throws<ObjectDisposedException>(()=>a.Revoke(b.DeviceId));
        Assert.Throws<IOException>(()=>DeviceVault.Open(store));
    }
    [Test]
    public void FailedCreateAndOpen_ReleaseOwnership()
    {
        var store=new MemoryProtectedStore{FailWrites=true};
        Assert.Throws<IOException>(()=>DeviceVault.Create(store));
        store.FailWrites=false;using(var created=DeviceVault.Create(store)){}
        Assert.Throws<InvalidOperationException>(()=>DeviceVault.Create(store));
        using var reopened=DeviceVault.Open(store);
    }
    [Test]
    public void DisposeNotifiesAllSessions_BeforeReleasingLease()
    {
        var store=new MemoryProtectedStore();using var a=DeviceVault.Create(store);using var b=DeviceVault.Create(new MemoryProtectedStore());
        SecureSessionTests.Approve(a,b,"peer");var closed=0;
        a.Revoked+=_=>throw new InvalidOperationException("failing observer");
        a.Revoked+=_=>{Assert.Throws<IOException>(()=>DeviceVault.Open(store));closed++;};
        a.Dispose();Assert.That(closed,Is.EqualTo(1));using var reopened=DeviceVault.Open(store);
    }
}
