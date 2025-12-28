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

    [Fact]
    public void StreamEncryptionDecryption()
    {
        var aes = new BytesEncryption(Enumerable.Repeat((byte)1, 16).ToArray());
        var msg = new BytesContainer("Hello world! Hello world! Hello world!");
        
        using var mem = new MemoryStream();
        using var cryptostream = new CryptoStream(mem, aes.GetEncryptor(), CryptoStreamMode.Write);
        cryptostream.Write(msg.GetBytes());
        cryptostream.FlushFinalBlock();
        mem.Seek(0, SeekOrigin.Begin);

        using var decoderestream = new CryptoStream(mem, aes.GetDecryptor(), CryptoStreamMode.Read);
        var buf = new byte[msg.GetBytes().Length];
        var read = 0;
        while(read < buf.Length)
            read += decoderestream.Read(buf, read, buf.Length - read);
        
        var newmsg = new BytesContainer(buf);
        
        Assert.Equal(msg, newmsg);
    }
}