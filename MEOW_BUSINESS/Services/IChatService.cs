using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IChatService
{
    Task<(bool, List<Exception>)> SendMessage(string message);
    
    List<MeowMessageText> GetChatMessages();

    void SetupNotificationsAndChatService();

    void SetupChatMessageReceivedAction(NotifyCollectionChangedEventHandler onMessage);

    int GetChatParticipantsCount();
    
    List<string> GetChatParticipantsNames();
}