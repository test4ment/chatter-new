using chatter_new.Messaging;

namespace chatter_new_tests;

public class BytesContainerTest
{
    [Fact]
    public void BytesContainerEquals()
    {
        var a = new BytesContainer("Text");
        var b = new BytesContainer("Text");
        
        Assert.Equal(a, b);
    }
}