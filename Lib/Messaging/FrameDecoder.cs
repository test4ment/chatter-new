using System.Buffers;

namespace chatter_new.Messaging;

public static class FrameDecoder
{
    public const int HeaderSize = sizeof(int);

    public static bool TryReadFrame(ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame, out SequencePosition consumed, out SequencePosition examined)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (TryReadFrame(ref reader, out frame))
        {
            consumed = buffer.GetPosition(reader.Consumed);
            examined = consumed;
            return true;
        }

        consumed = buffer.Start;
        examined = buffer.End;
        return false;
    }

    public static bool TryReadFrame(ref SequenceReader<byte> reader, out ReadOnlySequence<byte> frame)
    {
        var headerStart = reader.Consumed;
        
        if (!reader.TryReadBigEndian(out int length)) // reference BytesHelper
        {
            frame = default;
            return false;
        }

        if (reader.Remaining < length)
        {
            reader.Rewind(HeaderSize);
            frame = default;
            return false;
        }

        frame = reader.Sequence.Slice(headerStart + HeaderSize, length);
        reader.Advance(length);
        return true;
    }
}