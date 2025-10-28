namespace MEOW.Components.Models;

public interface IMessage
{
    MessageType Type { get; }
    public string Sender { get; set; }
}

public enum MessageType
{
    CONNECTED,
    DISCONNECTED,
    GPS,
    TASK,
    TEXT,
}

public abstract class MeowMessage : IMessage
{
    public abstract MessageType Type { get; }
    public string Sender { get; set; }

    protected MeowMessage(string sender)
    {
        Sender = sender;
    }
}

public class MeowMessageText : MeowMessage
{
    public override MessageType Type => MessageType.TEXT;
    public string Msg { get; set; }
    public bool HasSeen { get; set; } = false;

    public MeowMessageText(string msg, string sender) : base(sender)
    {
        Msg = msg;
    }
}

public class MeowMessageGps : MeowMessage
{
    public override MessageType Type => MessageType.GPS;
    public float Longitude { get; set; }
    public float Latitude { get; set; }

    public MeowMessageGps(string sender, float longitude, float latitude) : base(sender)
    {
        Longitude = longitude;
        Latitude = latitude;
    }
}
