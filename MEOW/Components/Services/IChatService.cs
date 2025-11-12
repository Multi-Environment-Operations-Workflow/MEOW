using System.Collections.Specialized;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IChatService
{
    Task<(bool, List<Exception>)> SendMessage(string message);
    List<MeowMessageText> GetChatMessages();

    void SetupNotificationsAndChatService();

    void SetupChatMessageReceivedAction(NotifyCollectionChangedEventHandler onMessage);

    int GetChatParticipantsCount();
    List<string> GetChatParticipantsNames();
}