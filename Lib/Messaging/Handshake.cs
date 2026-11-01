using chatter_crypto;

namespace chatter_new.Messaging;

[Obsolete("Should be done manually using DHHandshake message")]
public class DHHandshake(Protocol proto)
{
    public async Task<UniversalEncryption> Perform(CancellationToken ct = default)
    {
        using var keyExchange = new DHKeyExchange();

        await proto.Send(keyExchange.PublicKey, ct);

        var frame = await proto.ReadNextFrameAsync(ct)
            ?? throw new EndOfStreamException("Peer closed the connection during the key exchange.");

        var sharedKey = keyExchange.DerivePrivateKey(frame);
        return new UniversalEncryption(sharedKey, false);
    }
}
