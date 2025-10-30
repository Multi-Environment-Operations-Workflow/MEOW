using System.Text;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class MessageService(IBluetoothService bluetooth, IUserStateService userStateService) : IMessageService
{
    private readonly List<MeowMessage> _messages = new();
    private MessageSerializer _serializer = new();

    public async Task<(bool, List<Exception>)> SendMessage(string message)
    {
        if (message is null)
        {
            return (false, [new Exception("No message")]);
        }

        MeowMessageText _message = new(message, userStateService.GetName());
        var bytes = _serializer.Serialize(_message);

        var (anySuccess, allErrors) = await bluetooth.SendToAllAsync(bytes).ConfigureAwait(false);
        return (anySuccess, allErrors);
    }

    public void SetupMessageReceivedAction<T>(Action<T> onMessage) where T : MeowMessage
    {
        bluetooth.DeviceDataReceived += (receivedData) =>
        {
            try
            {
                var message = _serializer.Deserialize(receivedData);
                _messages.Add(message);

                if (message is T typedMessage)
                {
                    onMessage((T)message);
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

    public List<T> GetMessages<T>() where T : MeowMessage
    {
        return _messages.FindAll(m => m is T).Cast<T>().ToList();
    }
}
