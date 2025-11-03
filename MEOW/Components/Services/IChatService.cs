using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IChatService
{
    void Initialize();
    
    Task<(bool, List<Exception>)> SendMessage(string message);

    void SetupChatMessageReceivedAction(Action<MeowMessageText> onMessage);

    List<MeowMessageText> GetChatMessages();

    int GetChatParticipantsCount();
}