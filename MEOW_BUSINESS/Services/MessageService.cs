
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public class MessageService(IBluetoothService bluetooth, IErrorService errorService) : IMessageService
{
    static int MessageCount { get; set; }
    
    private readonly List<MeowMessage> _messages = new();
    
    public static int GetMessageCount()
    {
        return MessageCount++;
    }
    
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

                if (_messages.Any(m => m.MessageNumber == message.MessageNumber
                                       && m.UserId == message.UserId))
                {
                    return;
                }

                _messages.Add(message);
                RedistributeMessageToAllNodes(message);

            }
            catch (Exception ex)
            {
                errorService.Add(ex);
            }
        };

    }
    
    private void RedistributeMessageToAllNodes(MeowMessage message)
    {
        bluetooth.SendToAllAsync(message.Serialize());
    }
    
    /// <summary>
    /// Gets the list of connected devices.
    /// </summary>
    /// <returns>>A list of connected MeowDevice instances.</returns>
    public List<MeowDevice> GetConnectedDevices()
    {
        return bluetooth.GetConnectedDevices();
    }
    
    public List<T> GetMessages<T>() where T : MeowMessage
    {
        return _messages.FindAll(m => m is T).Cast<T>().ToList();
    }
}
