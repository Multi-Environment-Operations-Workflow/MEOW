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

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("Type", out var typeElement))
        {
            int typeNumber;
            typeElement.TryGetInt32(out typeNumber);

            MessageType type = (MessageType)typeNumber;

            return type switch
            {
                MessageType.TEXT => JsonSerializer.Deserialize<MeowMessageText>(json) ?? throw new InvalidDataException("Failed to deserialize TEXT message."),
                MessageType.GPS => JsonSerializer.Deserialize<MeowMessageGps>(json) ?? throw new InvalidDataException("Failed to deserialize GPS message."),
                _ => throw new NotSupportedException($"Message type {type} is not supported")
            };
        }

        throw new InvalidDataException("Invalid message format: Type property not found");
    }
}
