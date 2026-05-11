using System.Security.Cryptography;
using System.Text.Json;
using chatter_crypto;

namespace chatter_new_auth;

public sealed class Identity: IDisposable
{
    private readonly RSA rsa;
    public Identity() => rsa = RSA.Create();
    private Identity(RSA rsa) => this.rsa = rsa;

    public byte[] PublicKey => rsa.ExportSubjectPublicKeyInfo();

    public byte[] Encrypt(byte[] data) 
        => rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    public byte[] Decrypt(byte[] data)
        => rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);

    public string Serialize(byte[] password)
    {
        var privateKeyRaw = rsa.ExportPkcs8PrivateKey();
        try {
            var encryptor = new UniversalEncryption(password, true);
            
            var @private = encryptor.Encrypt(privateKeyRaw);
            
            var rec = new IdentityJSON(@private);
            return JsonSerializer.Serialize(rec);
        }
        finally { Array.Clear(privateKeyRaw); }
    }
    
    /// <exception cref="AuthenticationTagMismatchException">Provided password is invalid</exception>>
    public static Identity FromJSON(string json, byte[] password)
    {
        var rec = JsonSerializer.Deserialize<IdentityJSON>(json)!;
        var decryptor = new UniversalEncryption(password, true);

        var @private = decryptor.Decrypt(rec.privateKeyEnc);
        try {
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(@private, out _);
            
            return new Identity(rsa);
        }
        finally { Array.Clear(@private); }

    }
    
    internal record IdentityJSON(byte[] privateKeyEnc);

    public void Dispose()
    {
        rsa.Dispose();
    }
}