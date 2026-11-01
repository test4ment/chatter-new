using System.Security.Cryptography;
using chatter_crypto;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class DoubleRatchetTest
{
    [Fact]
    public void DoubleRatchetEncryption()
    {
        // data1, data2, data3, data4, resync, data5
        // having second key allow to read everything before resync

        var sharedKey = new byte[4];
        
        var ratchetA = new DoubleRatchet(sharedKey);
        var ratchetB = new DoubleRatchet(sharedKey);

        var data = "data1".Encode();
        var enc1 = ratchetA.Encrypt(data);
        var dec1 = ratchetB.Decrypt(enc1);
        Assert.Equal(data, dec1);
        ratchetA = ratchetA.Advance();
        ratchetB = ratchetB.Advance();

        data = "data2".Encode();
        var enc2 = ratchetB.Encrypt(data);
        var dec2 = ratchetA.Decrypt(enc2);
        Assert.Equal("data2".Encode(), dec2);
    }

    [Fact]
    public void DoubleRatchetResyncResetsState()
    {
        var ratchet = new DoubleRatchet(new byte[4]);
        var data = "resync-me".Encode();

        var cipher = ratchet.Encrypt(data);
        Assert.Equal(data, ratchet.Decrypt(cipher));
        ratchet = ratchet.Advance();
        Assert.ThrowsAny<CryptographicException>(() => ratchet.Decrypt(cipher));
        Assert.Equal(1u, ratchet.Counter);

        // resync with the new keying material resets 
        var newRatchet = new DoubleRatchet([1, 1, 1, 1]);
        Assert.Equal(0u, newRatchet.Counter);

        var cipher2 = newRatchet.Encrypt(data);
        Assert.Equal(data, newRatchet.Decrypt(cipher2));
        Assert.ThrowsAny<CryptographicException>(() => newRatchet.Decrypt(cipher));
    }
}
