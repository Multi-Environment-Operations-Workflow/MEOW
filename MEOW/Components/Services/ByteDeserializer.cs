namespace MEOW.Components.Services;

using System.Text;
using MEOW.Components.Models;

public class ByteDeserializer(byte[] payload, IErrorService errorService)
{
    private int _offset = 0;
    private readonly byte[] _payload = payload;

    public MeowMessage Deserialize()
    {
        MessageType type = (MessageType)ReadByte();
        byte senderLength = ReadByte();
        string sender = ReadString(senderLength);

        return type switch
        {
            MessageType.TEXT => DeserializeTextMessage(sender),
            MessageType.TASK => DeserializeTaskMessage(sender),
            MessageType.GPS => DeserializeGpsMessage(sender),
            _ => DeserializeUnsupportedMessage(type)
        };
    }
    private MeowMessage DeserializeUnsupportedMessage(MessageType type)
    {
        var exception = new NotSupportedException($"Message type {type} is not supported");
        errorService.Add(exception);
        throw exception;
    }
    
    private MeowMessageText DeserializeTextMessage(string sender)
    {
        string message = ReadLenghtPrefixedString();
        return new MeowMessageText(message, sender);
    }

    private MeowMessageTask DeserializeTaskMessage(string sender)
    {
        string title = ReadLenghtPrefixedString();
        string textContext = ReadLenghtPrefixedString();
        string fileData = ReadLenghtPrefixedString();
        return new MeowMessageTask(sender, title, textContext, fileData);
    }

    private MeowMessageGps DeserializeGpsMessage(string sender)
    {
        float longitude = ReadSingle();
        float latitude = ReadSingle();
        return new MeowMessageGps(sender, longitude, latitude);
    }

    private byte ReadByte() => _payload[_offset++];

    private int ReadInt32()
    {
        int value = BitConverter.ToInt32(_payload, _offset);
        _offset += 4;
        return value;
    }

    private float ReadSingle()
    {
        float value = BitConverter.ToSingle(_payload, _offset);
        _offset += 4;
        return value;
    }

    private string ReadString(int length)
    {
        string value = Encoding.UTF8.GetString(_payload, _offset, length);
        _offset += length;
        return value;
    }

    private string ReadLenghtPrefixedString()
    {
        int length = ReadInt32();
        return ReadString(length);
    }
}