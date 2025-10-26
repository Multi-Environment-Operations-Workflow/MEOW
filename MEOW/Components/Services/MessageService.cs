using System.Text;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class MessageService(IBluetoothService bluetooth, IUserStateService userStateService) : IMessageService
{
    private readonly List<string> _messages = new();

    public async Task<(bool, List<Exception>)> SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, [new Exception("Message is empty")]);
        }
        var bytes = Encoding.UTF8.GetBytes(userStateService.GetName() + ": " + message);
        var result = await bluetooth.SendToAllAsync(bytes).ConfigureAwait(false);
        return (result.anySuccess, result.allErrors);
    }

    public void SetupMessageReceivedAction(Action<string> onMessage)
    {
        bluetooth.DeviceDataReceived += (receivedData) =>
        {
            var message = Encoding.UTF8.GetString(receivedData);
            onMessage(message);
        };
    }

    public int GetParticipantsCount()
    {
        return bluetooth.GetConnectedDevicesCount() + 1;
    }

    public List<string> GetMessages() => _messages;
}
