using System.Security.Cryptography;
using chatter_crypto;
using chatter_new_auth;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class Encryption
{
    [Fact]
    public void ExchangeKeys()
    {
        using var alice = new DHKeyExchange();
        using var bob = new DHKeyExchange();

        var shared_alice = alice.DerivePrivateKey(bob.PublicKey);
        var shared_bob = bob.DerivePrivateKey(alice.PublicKey);

        Assert.Equal(shared_bob, shared_alice);
    }
    [Fact]
    public void SymmetricEncryption()
    {
        var enc = new UniversalEncryption("pw".Encode(), false); // should be true for passwords, lets save some electricity
        var text = "realHumanBean".Encode();
        
        var msg = enc.Encrypt(text);
        var msg2 = enc.Encrypt(text);
        Assert.NotEqual(msg, msg2); // same encryption gives different outputs...
        
        var decrypted = enc.Decrypt(msg);
        var decrypted2 = enc.Decrypt(msg2); // but decrypts into the same
        
        Assert.Equal(text, decrypted);
        Assert.Equal(text, decrypted2);
    }
    
    [Fact]
    public void EncryptionWSharedSecret()
    {
        using var alice = new DHKeyExchange();
        using var bob = new DHKeyExchange();

        var shared_alice = alice.DerivePrivateKey(bob.PublicKey);
        var shared_bob = bob.DerivePrivateKey(alice.PublicKey);
        
        Assert.Equal(shared_alice, shared_bob);
        
        var msg = "Hello world! Hello world! Hello world!".Encode();

        var alice_enc = new UniversalEncryption(shared_alice, false);
        var encrypted = alice_enc.Encrypt(msg);
        
        var bob_dec = new UniversalEncryption(shared_bob, false);
        var decrypted = bob_dec.Decrypt(encrypted);

        Assert.Equal(msg, decrypted);
        Assert.NotEqual(msg, encrypted);
    }
}