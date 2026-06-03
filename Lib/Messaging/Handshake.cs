using chatter_crypto;

namespace chatter_new.Messaging;

public class DHHandshake(Protocol proto)
{
    public async Task<UniversalEncryption> Perform(CancellationToken ct = default)
    {
        using var keyExchange = new DHKeyExchange();

        await proto.Send(keyExchange.PublicKey, ct);
        await proto.Receive(ct);

        var frame = await proto.GetNextFrame(ct);

        var sharedKey = keyExchange.DerivePrivateKey(frame);
        return new UniversalEncryption(sharedKey, false);
    }
}
