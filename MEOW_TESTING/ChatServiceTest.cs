using MEOW_BUSINESS.Services;
using MEOW_TESTING.Mocks;
using Moq;

namespace MEOW_TESTING;

public class ChatServiceTest
{
    [Fact]
    public async Task SendMessageTest()
    {
        // Mock the BluetoothService to simulate successful sending
        var bluetoothService = new Mock<IBluetoothService>();
        bluetoothService.Setup(bs => bs.SendToAllAsync(It.IsAny<byte[]>())).ReturnsAsync((true, []));
        var errorService = new TestErrorService();
        var messageService = new MessageService(bluetoothService.Object, errorService);
        
        // Mock the MeowPreferences to provide a username
        var meowPreferences = new Mock<IMeowPreferences>();
        meowPreferences.Setup(p => p.Get("username", "")).Returns("TestUser");
        
        var userStateService = new UserStateService(meowPreferences.Object);
        var notificationManagerService = new Mock<INotificationManagerService>();
        var chatService = new ChatService(messageService, userStateService, notificationManagerService.Object, errorService);
        
        // Send a message and verify the result
        var result = await chatService.SendMessage("Hello, World!");
        Assert.True(result.Item1);
        Assert.Empty(result.Item2);
    }
}