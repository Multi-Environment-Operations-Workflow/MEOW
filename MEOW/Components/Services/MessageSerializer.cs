using System.Text;
using System.Text.Json;
using MEOW.Components.Models;
using MEOW.Components.Services;

namespace MEOW.Components.Services;

public class MessageSerializer : IMessageSerializer
{
    public byte[] Serialize(IMessage message)
    {
        var json = JsonSerializer.Serialize(message, message.GetType());
        return Encoding.UTF8.GetBytes(json);
    }

    public IMessage Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        throw new Exception("This should be json: " + json);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("Type", out var typeElement))
        {
            var typeString = typeElement.GetString();
            if (typeString is not null && Enum.TryParse<MessageType>(typeString, out var messageType))
                return messageType switch
                {
                    MessageType.TEXT => JsonSerializer.Deserialize<MeowMessageText>(json) ?? throw new InvalidDataException("Failed to deserialize TEXT message."),
                    MessageType.GPS => JsonSerializer.Deserialize<MeowMessageGps>(json) ?? throw new InvalidDataException("Failed to deserialize GPS message."),
                    _ => throw new NotSupportedException($"Message type {messageType} is not supported")
                };
        }

        throw new InvalidDataException("Invalid message format: Type property not found");
    }
}
