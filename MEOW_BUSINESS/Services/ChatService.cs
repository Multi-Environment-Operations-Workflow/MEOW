using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public class ChatService(
    IMessageService messageService,
    IUserStateService userStateService,
    INotificationManagerService notificationManagerService,
    IErrorService errorService)
    : IChatService
{
    private ObservableCollection<MeowMessageText> MeowMessageTexts { get; set; } = new();
    public Task<(bool, List<Exception>)> SendMessage(string message)
    {
        var meowMessage = new MeowMessageText(userStateService.GetId(), MessageService.GetMessageCount(), message, userStateService.GetName());
        MeowMessageTexts.Add(meowMessage);
        return messageService.SendMessage(meowMessage);
    }

    public void SetupNotificationsAndChatService()
    {
        messageService.SetupMessageReceivedAction<MeowMessageText>((msg) =>
        {
            try
            {
                MeowMessageTexts.Add(msg);
                notificationManagerService.SendNotification("New Chat Message", $"{msg.Sender}: {msg.Message}",
                    DateTime.Now.AddSeconds(1));
            }
            catch (Exception ex)
            {
                errorService.Add(ex);
            }
        });
    }

    public void SetupChatMessageReceivedAction(NotifyCollectionChangedEventHandler onMessage)
    {
        MeowMessageTexts.CollectionChanged -= onMessage;
        MeowMessageTexts.CollectionChanged += onMessage;
    }

    public List<MeowMessageText> GetChatMessages()
    {
        return MeowMessageTexts.ToList();
    }

    /// <summary>
    /// Gets the count of chat participants, including the local device.
    /// </summary>
    /// <returns>Count of connected devices +1 for yourself</returns>
    public int GetChatParticipantsCount()
    {
        return messageService.GetConnectedDevices().Count + 1;
    }

    /// <summary>
    /// Gets a list chat participant names.
    /// </summary>
    /// <returns>A list of connected device names.</returns>
    public List<string> GetChatParticipantsNames()
    {
        return messageService.GetConnectedDevices().Select(d => d.Name).ToList();
    }
}
