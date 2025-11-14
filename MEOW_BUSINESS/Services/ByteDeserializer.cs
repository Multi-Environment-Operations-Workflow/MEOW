namespace MEOW_BUSINESS.Services;

using System.Text;
using Models;

public class ByteDeserializer(byte[] payload, IErrorService errorService)
{
    private int _offset = 0;

    public MeowMessage Deserialize()
    {
        byte senderUserId = ReadByte();
        int messageNumber = ReadInt32();
        MessageType type = (MessageType)ReadByte();
        byte senderLength = ReadByte();
        string sender = ReadString(senderLength);

        return type switch
        {
            MessageType.TEXT => DeserializeTextMessage(senderUserId, messageNumber, sender),
            MessageType.TASK => DeserializeTaskMessage(senderUserId, messageNumber, sender),
            MessageType.GPS => DeserializeGpsMessage(senderUserId, messageNumber, sender),
            _ => DeserializeUnsupportedMessage(type)
        };
    }
    private MeowMessage DeserializeUnsupportedMessage(MessageType type)
    {
        var exception = new NotSupportedException($"Message type {type} is not supported \n {BitConverter.ToString(payload)}");
        errorService.Add(exception);
        throw exception;
    }

    private MeowMessageText DeserializeTextMessage(byte senderUserId, int messageNumber, string sender)
    {
        string message = ReadLenghtPrefixedString();
        return new MeowMessageText(senderUserId, messageNumber, message, sender);
    }

    private MeowMessageTask DeserializeTaskMessage(byte senderUserId, int messageNumber, string sender)
    {
        string title = ReadLenghtPrefixedString();
        string textContext = ReadLenghtPrefixedString();
        string fileData = ReadLenghtPrefixedString();
        return new MeowMessageTask(senderUserId, messageNumber, sender, title, textContext, fileData);
    }

    private MeowMessageGps DeserializeGpsMessage(byte senderUserId, int messageNumber, string sender)
    {
        float longitude = ReadSingle();
        float latitude = ReadSingle();
        return new MeowMessageGps(senderUserId, messageNumber, sender, longitude, latitude);
    }

    private byte ReadByte() => payload[_offset++];

    /// <summary>
    /// Reads a 32-bit integer (4 bytes) from the payload.
    /// </summary>
    /// <returns>The integer read from the payload.</returns>
    private int ReadInt32()
    {
        int value = BitConverter.ToInt32(payload, _offset);
        _offset += 4;
        return value;
    }

    /// <summary>
    /// Reads a single-precision floating point number (4 bytes) from the payload.
    /// </summary>
    /// <returns>The float read from the payload.</returns>
    private float ReadSingle()
    {
        float value = BitConverter.ToSingle(payload, _offset);
        _offset += 4;
        return value;
    }

    private string ReadString(int length)
    {
        string value = Encoding.UTF8.GetString(payload, _offset, length);
        _offset += length;
        return value;
    }

    /// <summary>
    /// Reads a string given the length of the string as a prefix Int32.
    /// </summary>
    /// <returns>The string read from the payload.</returns>
    private string ReadLenghtPrefixedString()
    {
        int length = ReadInt32();
        return ReadString(length);
    }
}
