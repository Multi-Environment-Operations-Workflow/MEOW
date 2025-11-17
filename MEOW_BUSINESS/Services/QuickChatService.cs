using MEOW_BUSINESS.Models;
using MEOW_BUSINESS.Enums;

namespace MEOW_BUSINESS.Services;

public class QuickChatService (IMessageService messageService, 
    IUserStateService userStateService, 
    IErrorService errorService,
    INotificationManagerService notificationManagerService
    ) : IQuickChatService
{
    
    private static string MessageFromByte(QuickChatMessageType messageType)
        => messageType switch
        {
            QuickChatMessageType.Help => "SOS: Need Help!",
            QuickChatMessageType.Found => "Found The Person!",
            _ => "Error sending quick chat message"
        };

    public void SetupReceiveMessages()
    {
        messageService.SetupMessageReceivedAction<MeowMessageQuickChat>(QuickChatMessageReceivedAction);
    }
    
    private void QuickChatMessageReceivedAction(MeowMessageQuickChat quickChat)
    {
        try
        {
            notificationManagerService.SendNotification(
                "QuickChat Alert",
                $"{MessageFromByte(quickChat.QMessageType)} + {quickChat.Sender}",
                DateTime.Now
            );
            errorService.Add(new Exception($"{quickChat.Longitude} {quickChat.Latitude} {MessageFromByte(quickChat.QMessageType)} {quickChat.Sender}"));
        }
        catch (Exception ex)
        {
            errorService.Add(ex);
        }
    }

    
    public Task<(bool, List<Exception>)> SendMessage(QuickChat quickChat)
    {
        var meowMessage = new MeowMessageQuickChat(userStateService.GetId(), MessageService.GetMessageCount(), userStateService.GetName(),
            quickChat.Longitude, quickChat.Latitude, quickChat.Type);
        return messageService.SendMessage(meowMessage);
    }
}