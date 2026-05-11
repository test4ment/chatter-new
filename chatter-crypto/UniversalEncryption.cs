using System.Security.Cryptography;

namespace chatter_crypto;

public class UniversalEncryption(byte[] inputKeyingMaterial, bool passwordDerivation)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int SaltSize = 16;
    private const int IterCount = 800_000;
    
    public byte[] Encrypt(byte[] data)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipherText = new byte[data.Length];

        var key = KeyDerivation(in inputKeyingMaterial, passwordDerivation, in salt);
        
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, data, cipherText, tag);
        
        return salt.Concat(nonce).Concat(tag).Concat(cipherText).ToArray();
    }

    public byte[] Decrypt(byte[] combinedData)
    {
        var salt = combinedData[..SaltSize];
        var nonce = combinedData[SaltSize..(SaltSize + NonceSize)];
        var tag = combinedData[(SaltSize + NonceSize)..(SaltSize + NonceSize + TagSize)];
        var cipherText = combinedData[(SaltSize + NonceSize + TagSize)..];

        var key = KeyDerivation(in inputKeyingMaterial, passwordDerivation, in salt);

        using var aesGcm = new AesGcm(key, TagSize);
        var plainText = new byte[cipherText.Length];
        
        aesGcm.Decrypt(nonce, cipherText, tag, plainText);

        return plainText;
    }

    private static byte[] KeyDerivation(in byte[] inputKeyingMaterial, bool passwordDerivation, in byte[] salt)
    {
        var key = new byte[32];
        if (passwordDerivation) {
            Rfc2898DeriveBytes.Pbkdf2(inputKeyingMaterial, salt, key, IterCount, HashAlgorithmName.SHA256);
        }
        else {
            HKDF.DeriveKey(HashAlgorithmName.SHA256, inputKeyingMaterial, key, salt, null);
        }

        return key;
    }
}
