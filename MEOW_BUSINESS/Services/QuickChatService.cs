using MEOW.Components.Enums;
using MEOW.Components.Models;
using System.Collections.ObjectModel;

namespace MEOW.Components.Services;

public class QuickChatService (IMessageService messageService, IUserStateService userStateService, IErrorService errorService) : IQuickChatService
{
    //En function til at få bytes til string
    private static string MessageFromByte(QuickChatMessageType messageType)
    {
        switch(messageType)
        {
            case QuickChatMessageType.Help:
                return "Oh god. Oh shit. Oh fuck. Help me right now. Jesus FUCK";
            case QuickChatMessageType.Found:
                return "I found the guy. I might be him.";
            default:
                return "Jap. He ded.";
        }
    }
    
    //On quickchat function
    //Bruges til at håndtere når man modtager en besked gennem quickchat

    public void SetupReceiveMessages()
    {
        messageService.SetupMessageReceivedAction<MeowMessageQuickChat>(QuickChatMessageReceivedAction);
    }
    
    private void QuickChatMessageReceivedAction(MeowMessageQuickChat quickChat)
    {
        errorService.Add(new Exception(MessageFromByte(quickChat.QMessageType) + quickChat.Sender));
    }
    
    public Task<(bool, List<Exception>)> SendMessage(QuickChat quickChat)
    {
        var meowMessage = new MeowMessageQuickChat(userStateService.GetId(), MessageService.GetMessageCount(), userStateService.GetName(),
            quickChat.Longitude, quickChat.Latitude, quickChat.Type);

        return messageService.SendMessage(meowMessage);
    }
}