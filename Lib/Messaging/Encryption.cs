using System.Security.Cryptography;

namespace chatter_new.Messaging;

public class DHKeyExchange: IDisposable
{
    private Lazy<ECDiffieHellman> dh_inst = new(ECDiffieHellman.Create);
    public byte[] PublicKey => dh_inst.Value.PublicKey.ExportSubjectPublicKeyInfo();

    public byte[] DerivePrivateKey(byte[] publicKey)
    {
        using var remote = ECDiffieHellman.Create();
        remote.ImportSubjectPublicKeyInfo(publicKey, out _);

        return dh_inst.Value.DeriveKeyMaterial(remote.PublicKey);
    }
    public void Dispose()
    {
        dh_inst.Value.Dispose();
        dh_inst = null!;
    }
}

public class BytesEncryption(byte[] initKey)
{
    public byte[] Encrypt(byte[] data)
    {
        using var aes = InitAes();
        return aes.EncryptCbc(data, aes.IV);
    }

    public byte[] Decrypt(byte[] data)
    {
        using var aes = InitAes();
        return aes.DecryptCbc(data, aes.IV);
    }

    public ICryptoTransform GetEncryptor()
    {
        using var aes = InitAes();
        return aes.CreateEncryptor();
    }

    public ICryptoTransform GetDecryptor()
    {
        using var aes = InitAes();
        return aes.CreateDecryptor();
    }

    private Aes InitAes()
    {
        var aes = Aes.Create();
        aes.Key = initKey;
        aes.IV = HKDF.DeriveKey(HashAlgorithmName.SHA1, initKey, aes.IV.Length);
        
        return aes;
    }
}
