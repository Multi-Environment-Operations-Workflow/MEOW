using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IMessageService
{
    Task<(bool, List<Exception>)> SendMessage(MeowMessage message);

    void SetupMessageReceivedAction<T>(Action<T> onMessage) where T : MeowMessage;

    List<MeowDevice> GetConnectedDevices();

    List<T> GetMessages<T>() where T : MeowMessage;
}
