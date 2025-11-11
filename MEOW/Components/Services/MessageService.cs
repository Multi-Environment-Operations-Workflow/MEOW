using MEOW.Components.Models;

namespace MEOW.Components.Services;

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
