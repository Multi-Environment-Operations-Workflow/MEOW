using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IMessageService
{
    Task<(bool, List<Exception>)> SendMessage(string message);

    void SetupMessageReceivedAction<T>(Action<T> onMessage) where T : IMessage;

    int GetParticipantsCount();

    public List<T> GetMessages<T>() where T : IMessage;
}
