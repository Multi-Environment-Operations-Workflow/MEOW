using System.Text;
using System.Text.Json;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class MessageSerializer : IMessageSerializer
{
    // Serializes a MeowMessage to JSON and returns it as a byte array
    public byte[] Serialize(MeowMessage message)
    {
        var json = JsonSerializer.Serialize(message, message.GetType());
        return Encoding.UTF8.GetBytes(json);
    }

    // Deserializes a byte array into an object
    public MeowMessage Deserialize(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);

        // Tries to retrieve the Type field to know which class to deserialize to
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
                _ => throw new NotSupportedException($"Message Type {type} is not supported")
            };
        }

        throw new InvalidDataException("Invalid message format: Type property not found");
    }
}
