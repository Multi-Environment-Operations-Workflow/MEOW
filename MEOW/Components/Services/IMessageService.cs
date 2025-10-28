using MEOW.Components.Models;

namespace MEOW.Components.Services;

public interface IMessageService
{
    //Task<(bool, List<Exception>)> SendMessage(string message);

    Task<(bool, List<Exception>)> SendMessageTest(string message);

    //void SetupMessageReceivedAction(Action<string> onMessage);

    void SetupMessageReceivedActionTest<T>(Action<T> onMessage) where T : IMessage;

    int GetParticipantsCount();

    //List<string> GetMessages();
}
