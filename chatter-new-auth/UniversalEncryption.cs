using System.Security.Cryptography;

namespace chatter_new_auth;

// TODO: merge with BytesEncryption, maybe in crypto module
public class UniversalEncryption(byte[] rawPassword)
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

    private Aes InitAes()
    {
        var aes = Aes.Create();
        aes.Key = HKDF.DeriveKey(HashAlgorithmName.SHA1, rawPassword, aes.Key.Length);
        aes.IV = HKDF.DeriveKey(HashAlgorithmName.SHA1, rawPassword, aes.IV.Length);
        return aes;
    }
}
