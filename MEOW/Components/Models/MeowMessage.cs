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
