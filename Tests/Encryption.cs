using System.Security.Cryptography;
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

        Assert.All(
            shared_alice.Zip(shared_bob), 
            (tuple, _) => Assert.Equal(tuple.First, tuple.Second)
            );
    }

    [Fact]
    public void EcnryptedMsg()
    {
        using var alice = new DHKeyExchange();
        using var bob = new DHKeyExchange();

        var shared_alice = alice.DerivePrivateKey(bob.PublicKey);
        var shared_bob = bob.DerivePrivateKey(alice.PublicKey);
        
        var msg = new BytesContainer("Hello world! Hello world! Hello world!");

        var alice_enc = new BytesEncryption(shared_alice);
        var encrypted = alice_enc.Encrypt(msg.GetBytes());
        
        var bob_dec = new BytesEncryption(shared_bob);
        var decrypted = bob_dec.Decrypt(encrypted);

        Assert.Equal(msg, new BytesContainer(decrypted));
        Assert.NotEqual(msg, new BytesContainer(encrypted));
    }
}