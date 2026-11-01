using System.Security.Cryptography;
using System.Text;

namespace chatter_crypto;

public sealed class DoubleRatchet
{
    private static readonly byte[] RatchetInfo = "chatter-double-ratchet-state"u8.ToArray();
    private static readonly byte[] RatchetSalt = "chatter-ratchet-salt"u8.ToArray();
    
    private byte[] currentKey;
    private UniversalEncryption enc;

    public uint Counter { get; private set; }

    public DoubleRatchet(byte[] keyingMaterial) {
        currentKey = keyingMaterial;
        enc = new UniversalEncryption(currentKey, false);
    }

    public byte[] Encrypt(byte[] data)
        => enc.Encrypt(data);

    public byte[] Decrypt(byte[] data)
        => enc.Decrypt(data);

    public DoubleRatchet Advance() => 
        new(DeriveKey(currentKey)) {
            Counter = Counter + 1
        };

    private static byte[] DeriveKey(byte[] keyingMaterial)
    {
        var key = new byte[keyingMaterial.Length];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, keyingMaterial, key, RatchetSalt, RatchetInfo);
        return key;
    }
}
