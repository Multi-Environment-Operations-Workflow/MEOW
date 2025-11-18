using System.Text;
using MEOW_BUSINESS.Models;
using MEOW_BUSINESS.Services;
using MEOW_TESTING.Mocks;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

namespace MEOW_TESTING;

public class ByteDeserializerTest
{
    [Fact]
    public void Deserialize_SenderLengthExceedsBuffer_Throws()
    {
        var payload = Build(
            new byte[] { 1 },       // userId
            Int(5),                 // message number
            new byte[] { 1 },       // type = TEXT
            new byte[] { 100 },     // senderLength=100 bytes
            new byte[] { 65, 66 }   // only 2 bytes of actual data
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    [Fact]
    public void Deserialize_TruncatedInt32_Throws()
    {
        var payload = new byte[] {
        7,   // userId
        0, 1 // only 2 bytes of a 4-byte Int32
    };

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    [Fact]
    public void Deserialize_TruncatedFloat_Throws()
    {
        var payload = Build(
            new byte[] { 2 },
            Int(1),
            new byte[] { (byte)MEOW_BUSINESS.Models.MessageType.GPS },
            new byte[] { 1 }, new byte[] { (byte)'A' },  // sender
            new byte[] { 1, 2 }                          // only half of float
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    [Fact]
    public void Deserialize_InvalidLengthPrefixedString_Throws()
    {
        var payload = Build(
            new byte[] { 5 },
            Int(123),
            new byte[] { (byte)MEOW_BUSINESS.Models.MessageType.TEXT },
            new byte[] { 1 }, new byte[] { (byte)'A' }, // sender
            Int(30),                                    // message length
            new byte[] { 65, 65, 65, 65 }               // only 4 bytes
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    [Fact]
    public void Deserialize_InvalidType_Throws_WithErrorServiceException()
    {
        var errorService = new TestErrorService();

        var payload = Build(
            new byte[] { 1 },     // userId
            Int(12),              // message number
            new byte[] { 255 },   // invalid type
            new byte[] { 1 }, new byte[] { (byte)'X' } // sender
        );

        var ex = Assert.Throws<Exception>(() =>
            new ByteDeserializer(payload, errorService).Deserialize()
        );

        // The inner exception is the actual NotSupportedException thrown by ByteDeserializer
        Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("Message type 255 is not supported", ex.InnerException.Message);
    }

    [Fact]
    public void Deserialize_ZeroLengthSender_Works()
    {
        var payload = Build(
            new byte[] { 1 },
            Int(9),
            new byte[] { (byte)MEOW_BUSINESS.Models.MessageType.TEXT },
            new byte[] { 0 },         // sender length = 0
            Int(3), new byte[] { (byte)'a', (byte)'b', (byte)'c' }
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        MeowMessageText msg = deserializer.Deserialize() as MeowMessageText;

        Assert.NotNull(msg);
        Assert.Equal("", msg.Sender);
        Assert.Equal("abc", msg.Message);
    }

    [Fact]
    public void Deserialize_UnicodeSender_Works()
    {
        string sender = "Åke🐱";
        byte[] senderBytes = Encoding.UTF8.GetBytes(sender);

        var payload = Build(
            new byte[] { 1 },
            Int(42),
            new byte[] { (byte)MEOW_BUSINESS.Models.MessageType.TEXT },
            new byte[] { (byte)senderBytes.Length },
            senderBytes,
            Int(2), new byte[] { (byte)'O', (byte)'K' }
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        MeowMessageText msg = deserializer.Deserialize() as MeowMessageText;

        Assert.NotNull(msg);
        Assert.Equal(sender, msg.Sender);
    }

    [Fact]
    public void Deserialize_OffsetAdvancesCorrectly()
    {
        var error = new TestErrorService();

        // TEXT: userId(1) + msgNum(4) + type(1) + senderLength(1) + sender(3) + length(4) + "msg" (3)
        var payload = Build(
            new byte[] { 50 },        // userId
            Int(999),
            new byte[] { (byte)MEOW_BUSINESS.Models.MessageType.TEXT },
            new byte[] { 3 },
            new byte[] { (byte)'B', (byte)'O', (byte)'B' },
            Int(3),
            new byte[] { (byte)'h', (byte)'i', (byte)'!' }
        );

        var deserializer = new ByteDeserializer(payload, error);
        MeowMessageText msg = deserializer.Deserialize() as MeowMessageText;

        // No bytes should remain unread
        Assert.Equal(payload.Length, msg.Serialize().Length);
    }

    [Fact]
    public void Deserialize_TaskMessage_InvalidInnerString_Throws()
    {
        var payload = Build(
            new byte[] { 1 },
            Int(1),
            new byte[] { (byte)MEOW_BUSINESS.Models.MessageType.TASK },
            new byte[] { 1 },
            new byte[] { (byte)'X' },

            Int(2), new byte[] { (byte)'A' },           // title truncated
            Int(1), new byte[] { (byte)'a' },
            Int(1), new byte[] { (byte)'b' }
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());
        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    private byte[] Build(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    private byte[] Int(int v) => BitConverter.GetBytes(v);
    private byte[] Float(float f) => BitConverter.GetBytes(f);
}
