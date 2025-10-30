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
}

public class MeowMessageText(string msg, string sender) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.TEXT;
    public string Msg { get; set; } = msg;
    public bool HasSeen { get; set; } = false;
}

public class MeowMessageGps(string sender, float longitude, float latitude) : MeowMessage(sender)
{
    public override MessageType Type => MessageType.GPS;
    public float Longitude { get; set; } = longitude;
    public float Latitude { get; set; } = latitude;
}
