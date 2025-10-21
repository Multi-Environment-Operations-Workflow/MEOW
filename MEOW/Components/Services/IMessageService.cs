using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IMessageService
{
    Task<(bool, List<Exception>)> SendMessage(string message);
    
    void SetupMessageReceivedAction(Action<string> onMessage);
    
    int GetParticipantsCount();
}