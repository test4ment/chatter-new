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
        // identity provides public and private key
        var identityAlice = new Identity();
        
        // share public
        var bobReceived = identityAlice.PublicKey;
        var bobEncryption = new PublicIdentity(bobReceived);
        var bobMessage = "bobMessage".Encode();
        var bobMsgEncrypted = bobEncryption.Encrypt(bobMessage);
        
        // receive and read encrypted data
        Assert.Equal(identityAlice.Decrypt(bobMsgEncrypted), bobMessage);
    }
}