using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class MessageService(IBluetoothService bluetooth, IErrorService errorService) : IMessageService
{
    private readonly List<MeowMessage> _messages = new();

    // Sends a message using the bluetooth service
    public async Task<(bool, List<Exception>)> SendMessage(MeowMessage message)
    {
        if (message is null)
        {
            var exception = new ArgumentNullException(nameof(message), "Message cannot be null");
            errorService.Add(exception);
            return (false, [exception]);
        }

        //throw new Exception($"Sending {message}");

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
                var message = new ByteDeserializer(receivedData, errorService).Deserialize();
                _messages.Add(message);

                if (message is T typedMessage)
                {
                    onMessage(typedMessage);
                }
            }
            catch (Exception ex)
            {
                errorService.Add(ex);
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

    public List<T> GetMessages<T>() where T : MeowMessage
    {
        return _messages.FindAll(m => m is T).Cast<T>().ToList();
    }
}
