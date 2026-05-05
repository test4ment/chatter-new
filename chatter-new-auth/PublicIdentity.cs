using System.Security.Cryptography;

namespace chatter_new_auth;

public class PublicIdentity
{
    private readonly RSA rsa;
    public PublicIdentity(byte[] publicKey)
    {
        rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKey, out _);
    }
    public byte[] Encrypt(byte[] data) 
        => rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
}
