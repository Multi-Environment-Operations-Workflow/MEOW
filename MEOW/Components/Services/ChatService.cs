using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

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
        return messageService.SendMessage(meowMessage);
    }

    private void ChatMessageReceivedAction(MeowMessageText msg)
    {
        MeowMessageTexts.Add(msg);
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

    public int GetChatParticipantsCount()
    {
        return messageService.GetParticipantsCount();
    }

    public List<string> GetConnectedDeviceName()
    {
        return messageService.GetConnectedDeviceName();
    }
}
