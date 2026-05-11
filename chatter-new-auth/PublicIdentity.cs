using System.Security.Cryptography;

namespace chatter_new_auth;

public class PublicIdentity: IDisposable
{
    private readonly RSA rsa;
    public PublicIdentity(byte[] publicKey)
    {
        rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
    }
    public byte[] Encrypt(byte[] data) 
        => rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        rsa.Dispose();
    }
}
