using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class ChatService(IMessageService messageService) : IChatService
{
    public Task<(bool, List<Exception>)> SendChatMessage(string message)
    {
        throw new NotImplementedException();
    }

    public void SetupChatMessageReceivedAction(Action<MeowMessageText> onMessage)
    {
        messageService.SetupMessageReceivedActionTest(onMessage);
    }

    public int GetChatParticipantsCount()
    {
        throw new NotImplementedException();
    }
}