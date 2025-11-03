using System.Text;

namespace MEOW.Components.Models;

public enum MessageType
{
    CONNECTED,
    DISCONNECTED,
    GPS,
    TASK,
    TEXT,
}

public abstract class MeowMessage(string sender)
{
    public abstract MessageType Type { get; }
    public string Sender { get; set; } = sender;

    public virtual byte[] Serialize()
    {
        byte senderLength = (byte)Sender.Length;
        byte[] payload = new byte[2 + senderLength];

        byte[] senderSerialized = Encoding.UTF8.GetBytes(Sender);

        payload[0] = (byte)Type;
        payload[1] = senderLength;
        senderSerialized.CopyTo(payload, 2);

        return payload;
    }
}

public class MeowMessageText(string message, string sender) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.TEXT;
    public string Message { get; set; } = message;

    public override byte[] Serialize()
    {
        // Serialized base type
        byte[] _base = base.Serialize();
        // Message serialized to bytes
        byte[] message = Encoding.UTF8.GetBytes(Message);
        // Message.length converted to bytes
        byte[] message_length = BitConverter.GetBytes(message.Length);

        // Instanciate a byte[] that contains will contain the entire serialized message
        byte[] bytes = new byte[_base.Length + message_length.Length + message.Length];

        // Copy base to the bytes[]
        _base.CopyTo(bytes, 0);
        // Copy the message length to the byte[]
        message_length.CopyTo(bytes, _base.Length);

        message.CopyTo(bytes, _base.Length + message_length.Length);

        return bytes;
    }
}

public class MeowMessageGps(string sender, float longitude, float latitude) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.GPS;
    public float Longitude { get; set; } = longitude;
    public float Latitude { get; set; } = latitude;

    public override byte[] Serialize()
    {
        // Serialized base type
        byte[] _base = base.Serialize();

        // Serialize Longitude
        byte[] longitude = BitConverter.GetBytes(Longitude);

        // Serialize Latitude
        byte[] latitude = BitConverter.GetBytes(Latitude);

        byte[] bytes = new byte[_base.Length + longitude.Length + latitude.Length];

        _base.CopyTo(bytes, 0);
        longitude.CopyTo(bytes, _base.Length);
        latitude.CopyTo(bytes, _base.Length + longitude.Length);

        return bytes;
    }
}

public class MeowMessageTask(string sender, string title, string textContext, string fileData) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.TASK;

    public string Title { get; private set; } = title;
    public string TextContext { get; private set; } = textContext;
    public string FileData { get; private set; } = fileData;

    public override byte[] Serialize()
    {
        // Serialized base type
        byte[] _base = base.Serialize();

        byte[] titleBytes = Encoding.UTF8.GetBytes(Title);
        byte[] titleBytesLength = BitConverter.GetBytes(titleBytes.Length);

        byte[] textContextBytes = Encoding.UTF8.GetBytes(TextContext);
        byte[] textContextBytesLength = BitConverter.GetBytes(textContextBytes.Length);

        byte[] fileDataBytes = Encoding.UTF8.GetBytes(FileData);
        byte[] fileDataBytesLength = BitConverter.GetBytes(fileDataBytes.Length);

        // Each 4 is space for the 32-bit int lengths
        int totalMessageLength = _base.Length + 4 + titleBytes.Length + 4 + textContextBytes.Length + 4 + fileDataBytes.Length;
        byte[] bytes = new byte[totalMessageLength];

        int nextIndex = _base.Length;
        titleBytesLength.CopyTo(bytes, nextIndex);

        nextIndex += titleBytesLength.Length;
        titleBytes.CopyTo(bytes, nextIndex);

        nextIndex += titleBytes.Length;
        textContextBytesLength.CopyTo(bytes, nextIndex);

        nextIndex += textContextBytesLength.Length;
        textContextBytes.CopyTo(bytes, nextIndex);

        nextIndex += textContextBytes.Length;
        fileDataBytesLength.CopyTo(bytes, nextIndex);

        nextIndex += fileDataBytesLength.Length;
        fileDataBytes.CopyTo(bytes, nextIndex);

        return bytes;
    }
}
