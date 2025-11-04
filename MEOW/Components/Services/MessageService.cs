using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class MessageService(IBluetoothService bluetooth, IUserStateService userStateService) : IMessageService
{
    private readonly List<MeowMessage> _messages = new();

    // Sends a message using the bluetooth service
    public async Task<(bool, List<Exception>)> SendMessage(MeowMessage message)
    {
        if (message is null)
        {
            return (false, [new Exception("No message")]);
        }

        _messages.Add(message);

        var bytes = message.Serialize();

        var (anySuccess, allErrors) = await bluetooth.SendToAllAsync(bytes).ConfigureAwait(false);
        return (anySuccess, allErrors);
    }

    public void SetupMessageReceivedAction<T>(Action<T> onMessage) where T : MeowMessage
    {
        bluetooth.DeviceDataReceived += (receivedData) =>
        {
            try
            {
                var message = new ByteDeserializer(receivedData).Deserialize();
                _messages.Add(message);

                // Only send messages of type T to actions that wants that type
                // e.g if a service wants MeowMessageText, it will only receive those
                if (message is T typedMessage)
                {
                    onMessage(typedMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to deserialize message: {ex.Message}");
                throw new Exception($"Failed to deserialize message: {ex.Message}");
            }
        };

    }

    public int GetParticipantsCount()
    {
        return bluetooth.GetConnectedDevicesCount() + 1;
    }

    public List<string> GetConnectedDeviceName()
    {
        return bluetooth.GetConnectedDeviceName();
    }

    // Returns only messages of the specified types
    public List<T> GetMessages<T>() where T : MeowMessage
    {
        return _messages.FindAll(m => m is T).Cast<T>().ToList();
    }
}
