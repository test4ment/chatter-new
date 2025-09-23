using System.Text;

namespace chatter_new.Messaging;

public sealed record BytesContainer(string text)
{
    public BytesContainer(byte[] data): this(Encoding.Unicode.GetString(data))
    { }
    private readonly byte[] data = Encoding.Unicode.GetBytes(text);
    public byte[] GetBytes()
    {
        return data;
    }
    public override string ToString()
    {
        return text;
    }
}
