using MEOW_BUSINESS.Services;
using MEOW_TESTING.Mocks;
using NSubstitute;

namespace MEOW_TESTING;

public class ChatServiceTest
{
    [Fact]
    public async Task SendMessageTest()
    {
        // Mock the BluetoothService to simulate successful sending
        var bluetoothService = Substitute.For<IBluetoothService>();
        bluetoothService.BroadcastMessage(Arg.Any<byte[]>()).Returns((true, []));
        var errorService = new TestErrorService();
        var messageService = new MessageService(bluetoothService, errorService);
        
        // Mock the MeowPreferences to provide a username
        var meowPreferences = Substitute.For<IMeowPreferences>();
        meowPreferences.Get("username", "").Returns("TestUser");
        
        var userStateService = new UserStateService(meowPreferences);
        var notificationManagerService = Substitute.For<INotificationManagerService>();
        var chatService = new ChatService(messageService, userStateService, notificationManagerService, errorService);
        
        // Send a message and verify the result
        var result = await chatService.SendMessage("Hello, World!");
        Assert.True(result.Item1);
        Assert.Empty(result.Item2);
    }
}