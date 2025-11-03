using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IMessageService
{
    Task<(bool, List<Exception>)> SendMessage(MeowMessage message);

    void SetupMessageReceivedAction<T>(Action<T> onMessage) where T : MeowMessage;

    int GetParticipantsCount();

    public List<T> GetMessages<T>() where T : MeowMessage;

    public string GetSender();
}
