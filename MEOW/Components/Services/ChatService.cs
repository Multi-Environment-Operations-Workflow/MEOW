using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class ChatService(
    IMessageService messageService,
    IUserStateService userStateService,
    INotificationManagerService notificationManagerService)
    : IChatService
{
    List<MeowMessageText> MeowMessageTexts { get; set; } = new();

    public void Initialize()
    {
        messageService.SetupMessageReceivedAction<MeowMessageText>(ChatMessageReceivedAction);
    }
    
    public Task<(bool, List<Exception>)> SendMessage(string message)
    {
        var meowMessage = new MeowMessageText(message, userStateService.GetName());
        return messageService.SendMessage(meowMessage);
    }
    
    private void ChatMessageReceivedAction(MeowMessageText msg)
    {
        MeowMessageTexts.Add(msg);
        notificationManagerService.SendNotification("New Chat Message", $"{msg.Sender}: {msg.Message}", DateTime.Now.AddSeconds(1));
    }

    public void SetupChatMessageReceivedAction(Action<MeowMessageText> onMessage)
    {
        messageService.SetupMessageReceivedAction(onMessage);
    }
    
    public List<MeowMessageText> GetChatMessages()
    {
        return MeowMessageTexts;
    }

    public int GetChatParticipantsCount()
    {
        return messageService.GetParticipantsCount();
    }
}