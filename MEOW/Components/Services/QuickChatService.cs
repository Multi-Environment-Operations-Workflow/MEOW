using MEOW.Components.Enums;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class QuickChatService
{
    //En function til at få bytes til string
    public string MessageFromByte(QuickChatMessageType messageType)
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

    public void OnQuickChat(MeowMessageQuickChat msg)
    {
        
    }
}