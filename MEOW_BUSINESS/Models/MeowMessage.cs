using System.Text;

namespace MEOW_BUSINESS.Models;

public enum MessageType
{
    CONNECTED,
    DISCONNECTED,
    GPS,
    TASK,
    TEXT,
}

public abstract class MeowMessage(byte userId, int messageNumber, string sender)
{
    public byte UserId { get; set; } = userId;
    public int MessageNumber { get; set; } = messageNumber;
    public abstract MessageType Type { get; }
    public string Sender { get; set; } = sender;

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        SerializeCore(writer);
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the core properties of a MeowMessage.
    /// </summary>
    /// <param name="writer">The BinaryWriter to write the data to.</param>
    protected virtual void SerializeCore(BinaryWriter writer)
    {
        writer.Write(UserId);
        writer.Write(MessageNumber);
        writer.Write((byte)Type);
        writer.Write(Sender);
    }

    protected static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}

public class MeowMessageText(byte userId, int messageNumber, string message, string sender) : MeowMessage(userId, messageNumber, sender)
{
    public override MessageType Type => MessageType.TEXT;
    public string Message { get; set; } = message;

    protected override void SerializeCore(BinaryWriter writer)
    {
        base.SerializeCore(writer);
        WriteString(writer, Message);
    }
}

public class MeowMessageGps(byte userId, int messageNumber, string sender, float longitude, float latitude) : MeowMessage(userId, messageNumber, sender)
{
    public override MessageType Type => MessageType.GPS;
    public float Longitude { get; set; } = longitude;
    public float Latitude { get; set; } = latitude;

    protected override void SerializeCore(BinaryWriter writer)
    {
        base.SerializeCore(writer);
        writer.Write(Longitude);
        writer.Write(Latitude);
    }
}

public class MeowMessageTask(byte userId, int messageNumber, string sender, string title, string textContext, string fileData) : MeowMessage(userId, messageNumber, sender)
{
    public override MessageType Type => MessageType.TASK;

    public string Title { get; private set; } = title;
    public string? TextContext { get; private set; } = textContext;
    public string? FileData { get; private set; } = fileData;

    protected override void SerializeCore(BinaryWriter writer)
    {
        base.SerializeCore(writer);
        WriteString(writer, Title);
        WriteString(writer, TextContext ??= "");
        WriteString(writer, FileData ??= "");
    }
}
