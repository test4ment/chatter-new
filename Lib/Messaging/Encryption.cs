using System.Security.Cryptography;

namespace chatter_new.Messaging;

public class DHKeyExchange: IDisposable
{
    private Lazy<ECDiffieHellman> dh_inst = new(ECDiffieHellman.Create);
    public byte[] PublicKey => dh_inst.Value.PublicKey.ExportSubjectPublicKeyInfo();

    public byte[] DerivePrivateKey(byte[] remotePublicKey)
    {
        using var remote = ECDiffieHellman.Create();
        remote.ImportSubjectPublicKeyInfo(remotePublicKey, out _);

        return dh_inst.Value.DeriveKeyMaterial(remote.PublicKey);
    }
    public void Dispose()
    {
        dh_inst.Value.Dispose();
        dh_inst = null!;
    }
}

