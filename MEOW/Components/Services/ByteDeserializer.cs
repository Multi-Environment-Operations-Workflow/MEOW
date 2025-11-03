namespace MEOW.Components.Services
{
    using System.Text;
    using MEOW.Components.Models;

    public class ByteDeserializer
    {
        public static MeowMessage Deserialize(byte[] payload)
        {
            MessageType type = (MessageType)payload[0];

            // Get sender length and deserialize sender bytes
            byte senderLength = payload[1];
            string sender = Encoding.UTF8.GetString(payload, 2, senderLength);

            switch (type)
            {
                case MessageType.TEXT:
                    // Get message lenght and deserialize message length bytes
                    int messageLengthStart = 2 + senderLength;
                    int messageLength = BitConverter.ToInt32(payload, messageLengthStart);

                    // Find start of message and deserialize message
                    int messageStart = messageLengthStart + 4;
                    string message = Encoding.UTF8.GetString(payload, messageStart, messageLength);

                    return new MeowMessageText(message, sender);
                case MessageType.GPS:
                    int longitudeStart = 2 + senderLength;
                    float longitude = BitConverter.ToSingle(payload, longitudeStart);

                    int latitudeStart = longitudeStart + 4;
                    float latitude = BitConverter.ToSingle(payload, latitudeStart);

                    return new MeowMessageGps(sender, longitude, latitude);
                case MessageType.TASK:
                    // Title
                    int titleLengthStart = 2 + senderLength;
                    int titleLength = BitConverter.ToInt32(payload, titleLengthStart);

                    int titleStart = titleLengthStart + 4;
                    string title = Encoding.UTF8.GetString(payload, titleStart, titleLength);

                    // TextContext
                    int textContextLengthStart = titleStart + title.Length;
                    int textContextLength = BitConverter.ToInt32(payload, textContextLengthStart);

                    int textContextStart = textContextLengthStart + 4;
                    string textContext = Encoding.UTF8.GetString(payload, textContextStart, textContextLength);

                    // FileData
                    int fileDataLengthStart = textContextStart + textContext.Length;
                    int fileDataLength = BitConverter.ToInt32(payload, fileDataLengthStart);

                    int fileDataStart = fileDataLengthStart + 4;
                    string fileData = Encoding.UTF8.GetString(payload, fileDataStart, fileDataLength);

                    return new MeowMessageTask(sender, title, textContext, fileData);
                default:
                    return new MeowMessageText("Unsupported message type", "Error");
            }
        }
    }
}
