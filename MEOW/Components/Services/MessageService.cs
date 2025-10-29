using System.Text;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class MessageService(IBluetoothService bluetooth, IUserStateService userStateService) : IMessageService
{
    //private readonly List<string> _messages = new();
    private readonly List<MeowMessage> _typedMessages = new();
    private MessageSerializer _serializer = new MessageSerializer();

    /*
    public async Task<(bool, List<Exception>)> SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, [new Exception("Message is empty")]);
        }
        var bytes = Encoding.UTF8.GetBytes(userStateService.GetName() + ": " + message);

        //Result from sending message
        var (anySuccess, allErrors) = await bluetooth.SendToAllAsync(bytes).ConfigureAwait(false);
        return (anySuccess, allErrors);
    }
    */

    public async Task<(bool, List<Exception>)> SendMessageTest(string message)
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

    /*
    public void SetupMessageReceivedAction(Action<string> onMessage)
    {
        bluetooth.DeviceDataReceived += (receivedData) =>
        {
            var message = Encoding.UTF8.GetString(receivedData);
            onMessage(message);
        };
    }
    */

    public void SetupMessageReceivedActionTest<T>(Action<T> onMessage) where T : MeowMessage
    {
        bluetooth.DeviceDataReceived += (receivedData) =>
        {
            try
            {
                var message = _serializer.Deserialize(receivedData);
                _typedMessages.Add(message);

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

    //public List<string> GetMessages() => _messages;
}
