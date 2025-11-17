using System.Text;
using MEOW_BUSINESS.Models;
using MEOW_BUSINESS.Services;
using MEOW_TESTING.Mocks;

namespace MEOW_TESTING;

public class MeowMessageTest
{
    [Fact]
    public void SerializeDeserialize_TextMessage_Works()
    {
        MeowMessageText original = new(
                userId: 5,
                messageNumber: 12345,
                message: "Hello World",
                sender: "Alice"
                );

        byte[] data = original.Serialize();

        ByteDeserializer deserializer = new(data, new TestErrorService());
        MeowMessageText result = deserializer.Deserialize() as MeowMessageText;

        Assert.NotNull(result);
        Assert.Equal(original.UserId, result.UserId);
        Assert.Equal(original.MessageNumber, result.MessageNumber);
        Assert.Equal(original.Type, result.Type);
        Assert.Equal(original.Message, result.Message);
        Assert.Equal(original.Sender, result.Sender);
    }

    [Fact]
    public void SerializeDeserialize_TextMessage_WithUnicodeSender_Works()
    {
        MeowMessageText original = new(
            userId: 9,
            messageNumber: 1,
            message: "Hej världen!",
            sender: "Åke🐱"
        );

        byte[] data = original.Serialize();

        ByteDeserializer deserializer = new(data, new TestErrorService());
        MeowMessageText result = deserializer.Deserialize() as MeowMessageText;

        Assert.NotNull(result);
        Assert.Equal(original.Sender, result.Sender);
        Assert.Equal(original.Message, result.Message);
    }

    [Fact]
    public void SerializeDeserialize_TextMessage_EmptyString_Works()
    {
        MeowMessageText original = new(
            userId: 5,
            messageNumber: 10,
            message: "",
            sender: "Test"
        );

        byte[] data = original.Serialize();
        MeowMessageText result = new ByteDeserializer(data, new TestErrorService()).Deserialize() as MeowMessageText;

        Assert.NotNull(result);
        Assert.Equal("", result.Message);
    }

    [Fact]
    public void SerializeDeserialize_GpsMessage_Works()
    {
        MeowMessageGps original = new(
                userId: 5,
                messageNumber: 12345,
                sender: "Alice",
                longitude: 1.2345f,
                latitude: 6.789f
                );

        byte[] data = original.Serialize();

        ByteDeserializer deserializer = new(data, new TestErrorService());
        MeowMessageGps result = deserializer.Deserialize() as MeowMessageGps;

        Assert.NotNull(result);
        Assert.Equal(original.UserId, result.UserId);
        Assert.Equal(original.MessageNumber, result.MessageNumber);
        Assert.Equal(original.Type, result.Type);
        Assert.Equal(original.Sender, result.Sender);
        Assert.Equal(original.Longitude, result.Longitude);
        Assert.Equal(original.Latitude, result.Latitude);
    }

    [Fact]
    public void SerializeDeserialize_GpsMessage_FloatPrecisionMaintained()
    {
        float lon = 123.456789f;
        float lat = -987.654321f;

        MeowMessageGps original = new(
            userId: 1,
            messageNumber: 42,
            sender: "GPS",
            longitude: lon,
            latitude: lat
        );

        byte[] data = original.Serialize();

        MeowMessageGps result = new ByteDeserializer(data, new TestErrorService()).Deserialize() as MeowMessageGps;

        Assert.NotNull(result);
        Assert.Equal(lon, result.Longitude);
        Assert.Equal(lat, result.Latitude);
    }

    [Fact]
    public void SerializeDeserialize_TaskMessage_Works()
    {
        MeowMessageTask original = new(
                userId: 5,
                messageNumber: 12345,
                sender: "Alice",
                title: "abc",
                textContext: "def",
                fileData: "ghi"
                );

        byte[] data = original.Serialize();

        ByteDeserializer deserializer = new(data, new TestErrorService());
        MeowMessageTask result = deserializer.Deserialize() as MeowMessageTask;

        Assert.NotNull(result);
        Assert.Equal(original.UserId, result.UserId);
        Assert.Equal(original.MessageNumber, result.MessageNumber);
        Assert.Equal(original.Type, result.Type);
        Assert.Equal(original.Sender, result.Sender);
        Assert.Equal(original.Title, result.Title);
        Assert.Equal(original.TextContext, result.TextContext);
        Assert.Equal(original.FileData, result.FileData);
    }

    [Fact]
    public void SerializeDeserialize_TaskMessage_EmptyOptionalFields_Works()
    {
        MeowMessageTask original = new(
            userId: 2,
            messageNumber: 999,
            sender: "Worker",
            title: "Job",
            textContext: "",
            fileData: ""
        );

        byte[] data = original.Serialize();
        MeowMessageTask result = new ByteDeserializer(data, new TestErrorService()).Deserialize() as MeowMessageTask;

        Assert.NotNull(result);
        Assert.Equal("", result.TextContext);
        Assert.Equal("", result.FileData);
    }

    [Fact]
    public void SerializeDeserialize_TaskMessage_NullOptionalFields_Works()
    {
        // Pass null for optional fields
        MeowMessageTask original = new(
            userId: 5,
            messageNumber: 123,
            sender: "Alice",
            title: "abc",
            textContext: null,
            fileData: null
        );

        byte[] data = original.Serialize();

        var deserializer = new ByteDeserializer(data, new TestErrorService());
        MeowMessageTask result = deserializer.Deserialize() as MeowMessageTask;

        Assert.NotNull(result);
        Assert.Equal("", result.TextContext); // null -> ""
        Assert.Equal("", result.FileData);    // null -> ""
        Assert.Equal("abc", result.Title);
    }

    [Fact]
    public void Deserialize_InvalidType_Throws()
    {
        var payload = BuildPayload(
            new byte[] { 1 },                    // userId
            BitConverter.GetBytes(100),          // message number
            new byte[] { 255 },                  // invalid message type
            new byte[] { 3 },                    // senderLength
            Encoding.UTF8.GetBytes("Bob")        // sender
        );

        var deserializer = new ByteDeserializer(payload, new TestErrorService());

        // TestErrorService Exception
        var ex = Assert.Throws<Exception>(() => deserializer.Deserialize());

        // ByteDeserializer NotSupportedException
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void Deserialize_TruncatedPayload_Throws()
    {
        // valid beginning: userId, msgNum, type, senderLength = 4
        byte[] payload = {
        1,
        0,0,0,1,
        (byte)MessageType.TEXT,
        4,               // sender length
        (byte)'A', (byte)'B' // but missing 2 bytes
    };

        var deserializer = new ByteDeserializer(payload, new TestErrorService());

        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    [Fact]
    public void Deserialize_InvalidStringLength_Throws()
    {
        // userId, msgNum, type, senderLength = **100 bytes**, but only 3 follow
        byte[] payload = {
        1,
        0,0,0,1,
        (byte)MessageType.TEXT,
        100,
        (byte)'X', (byte)'Y', (byte)'Z'
    };

        var deserializer = new ByteDeserializer(payload, new TestErrorService());

        Assert.ThrowsAny<Exception>(() => deserializer.Deserialize());
    }

    private byte[] BuildPayload(params byte[][] parts)
    {
        return parts.SelectMany(p => p).ToArray();
    }
}
