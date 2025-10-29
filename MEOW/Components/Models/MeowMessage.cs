namespace MEOW.Components.Models;

public enum MessageType
{
    CONNECTED,
    DISCONNECTED,
    GPS,
    TASK,
    TEXT,
}

public abstract class MeowMessage
{
    public abstract MessageType Type { get; }
    public string Sender { get; set; }

    protected MeowMessage(string sender)
    {
        Sender = sender;
    }
}

public class MeowMessageText(string message, string sender) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.TEXT;
    public string Message { get; set; } = message;
    public bool HasSeen { get; set; } = false;
}

public class MeowMessageGps(string sender, float longitude, float latitude) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.GPS;
    public float Longitude { get; set; } = longitude;
    public float Latitude { get; set; } = latitude;
}
