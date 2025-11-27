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
        message.Time = DateTime.Now;
        //throw new Exception($"Sending {message}");

        
        _messages.Add(message);

        var bytes = message.Serialize();

        var (anySuccess, allErrors) = await bluetooth.BroadcastMessage(bytes);

        return (anySuccess, allErrors);
    }

    public void SetupMessageReceivedAction<T>(Action<T> onMessage) where T : MeowMessage
    {

        bluetooth.DeviceDataReceived += async (receivedData) =>
        {
            try
            {
                var message = new ByteDeserializer(receivedData, errorService).Deserialize();

                if (message is not T typedMessage)
                {
                    return;
                }
                message.Time = DateTime.Now;
                var deviceName = bluetooth.get_device_name_by_id(message.UserId);

                if (deviceName != null)
                {
                    message.latest_rssi = await bluetooth.GetRSSI(deviceName);
                       
                }


                if (_messages.Any(m => m.MessageNumber == message.MessageNumber
                                       && m.UserId == message.UserId))
                {
                    return;
                }
                
                onMessage(typedMessage);

                RedistributeMessageToAllNodes(message);
                _messages.Add(message);
            }
            catch (Exception ex)
            {
                errorService.Add(ex);
            }
        };

    }
    
    private void RedistributeMessageToAllNodes(MeowMessage message)
    {
        bluetooth.BroadcastMessage(message.Serialize());
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
