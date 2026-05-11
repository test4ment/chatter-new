using System.Security.Cryptography;

namespace chatter_crypto;

public class DHKeyExchange: IDisposable
{
    private ECDiffieHellman dh_inst = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);
    public byte[] PublicKey => dh_inst.PublicKey.ExportSubjectPublicKeyInfo();

    public byte[] DerivePrivateKey(byte[] remotePublicKey)
    {
        using var remote = ECDiffieHellman.Create();
        remote.ImportSubjectPublicKeyInfo(remotePublicKey, out _);

        return dh_inst.DeriveKeyMaterial(remote.PublicKey);
    }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        dh_inst.Dispose();
        dh_inst = null!;
    }
}
