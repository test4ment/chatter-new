using System.Security.Cryptography;
using System.Text.Json;

namespace chatter_new_auth;

public class Identity
{
    private readonly RSA rsa;
    public Identity() => rsa = RSA.Create();
    private Identity(RSA rsa) => this.rsa = rsa;

    public byte[] PublicKey => rsa.ExportRSAPublicKey();

    public byte[] Encrypt(byte[] data) 
        => rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    public byte[] Decrypt(byte[] data)
        => rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);

    public string Serialize(byte[] password)
    {
        var encryptor = new UniversalEncryption(password);
        rsa.ExportRSAPrivateKeyPem();
        var @private = encryptor.Encrypt(rsa.ExportRSAPrivateKey());
        var @public = encryptor.Encrypt(rsa.ExportRSAPublicKey());
        // RSA.Create(new )
        var rec = new IdentityJSON(@private, @public);
        return JsonSerializer.Serialize(rec);
    }

    public static Identity FromJSON(string json, byte[] password)
    {
        var rec = JsonSerializer.Deserialize<IdentityJSON>(json)!;
        var decryptor = new UniversalEncryption(password);
        
        var @private = decryptor.Decrypt(rec.privateKeyEnc);
        var @public = decryptor.Decrypt(rec.publicKeyEnc);
        
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(@public, out _);
        rsa.ImportRSAPrivateKey(@private, out _);
        
        return new Identity(rsa);
    }
    
    internal record IdentityJSON(byte[] privateKeyEnc, byte[] publicKeyEnc);
}