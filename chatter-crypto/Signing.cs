using System.Security.Cryptography;

namespace chatter_crypto;

public class Signing
{
    private readonly RSA rsa = RSA.Create();
    public byte[] PublicKey => rsa.ExportRSAPublicKey();

    public byte[] Sign(byte[] data) {
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
    public static bool Verify(byte[] data, byte[] signature, RSA remoteRSA)
    {
        return remoteRSA.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
    public static bool Verify(byte[] data, byte[] signature, byte[] remotePubKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(remotePubKey, out _);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
