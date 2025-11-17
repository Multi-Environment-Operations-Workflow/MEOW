using MEOW.Components.Enums;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IQuickChatService
{
    void SetupReceiveMessages();

    Task<(bool, List<Exception>)> SendMessage(QuickChat quickChat);
}