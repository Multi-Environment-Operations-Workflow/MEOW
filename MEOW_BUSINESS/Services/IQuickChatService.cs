namespace MEOW_BUSINESS.Services;

using Models;

public interface IQuickChatService
{
    void SetupReceiveMessages();

    Task<(bool, List<Exception>)> SendMessage(QuickChat quickChat);
}