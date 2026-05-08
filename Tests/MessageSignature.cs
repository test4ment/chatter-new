using chatter_crypto;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class MessageSignature
{
    [Fact]
    public void SignatureSuccess()
    {
        var alice = new Signing();
        // var bob = new Signing();

        var msg = "Hi Bob!".Encode();
        var msgSignature = alice.Sign(msg);

        var bobKnows = alice.PublicKey;
        Assert.True(Signing.Verify(msg, msgSignature, bobKnows));
    }

    [Fact]
    public void SignatureFailureMessageTamper()
    {
        var alice = new Signing();
        // var bob = new Signing();

        var msg = "Hi Bob!".Encode();
        var msgSignature = alice.Sign(msg);
        
        msg = "Hi Helga!".Encode(); // tampered!

        var bobKnows = alice.PublicKey;
        Assert.False(Signing.Verify(msg, msgSignature, bobKnows));
    }

    [Fact]
    public void SignatureFailureSignTamper()
    {
        var alice = new Signing();
        // var bob = new Signing();

        var msg = "Hi Bob!".Encode();
        var msgSignature = alice.Sign(msg);

        msgSignature[0] += 1;
        
        var bobKnows = alice.PublicKey;
        Assert.False(Signing.Verify(msg, msgSignature, bobKnows));
    }
}