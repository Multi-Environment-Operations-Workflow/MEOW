using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IChatService
{
    Task<(bool, List<Exception>)> SendChatMessage(string message);

    void SetupChatMessageReceivedAction(Action<MeowMessageText> onMessage);

    int GetChatParticipantsCount();
}