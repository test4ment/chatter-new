using System.Security.Cryptography;
using chatter_new_auth;
using chatter_new.Messaging;

namespace chatter_new_tests;

public class Authentification
{
    [Fact]
    public void CreateIdentityWithPassword()
    {
        var identity = new Identity();
        var pw = "password".Encode();
        var saved = identity.Serialize(pw);
        // create identity with password

        // encrypt some personal data
        var somedata = "somedata".Encode();
        var somedataEncrypted = identity.Encrypt(somedata);
        // load identity with password
        var loadedIdentity = Identity.FromJSON(saved, pw);
        
        // encryption still works
        var newEnc = loadedIdentity.Encrypt(somedata);
        Assert.Equal(identity.Decrypt(newEnc), somedata);
        
        // decrypt some personal data
        var somedataDecrypted = loadedIdentity.Decrypt(somedataEncrypted);

        Assert.Equal(somedata, somedataDecrypted);
    }

    [Fact]
    public void PublicKeyAuthentity()
    {
        var identity = new Identity();
        
        
        // identity provides public and private key
        // share public
        // receive encrypted data
        // response with normal answer
    }
}