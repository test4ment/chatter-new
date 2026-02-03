using System.Buffers.Binary;
using System.Text;

namespace chatter_new.Messaging;

public static class BytesHelper
{
    private static Encoding Encoding = Encoding.Unicode;
    public static byte[] Encode(this string data) => Encoding.GetBytes(data);
    public static byte[] Encode(this int num)
    {
        var buf = new byte[4]; 
        BinaryPrimitives.WriteInt32BigEndian(buf, num);
        return buf;
    }
    
    public static string Decode(this byte[] data) => Encoding.GetString(data);
    public static int DecodeInt(this byte[] data) 
        => BinaryPrimitives.ReadInt32BigEndian(data.AsSpan()[..sizeof(int)]);
    public static int DecodeInt(this List<byte> data)
        => DecodeInt(data[..sizeof(int)].ToArray());
}
