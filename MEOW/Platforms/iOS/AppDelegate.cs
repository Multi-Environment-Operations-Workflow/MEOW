using Foundation;

namespace MEOW;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate() //Changes here should be reflected in Platforms/iOS/AppDelegate.cs
    {
        base.OnCreate();
        var chatService = IPlatformApplication.Current?.Services.GetService<IChatService>();
        // I think this should work for IOS - Theis
        //var pinService = IPlatformApplication.Current?.Services.GetService<IPinService>();

        chatService?.SetupNotificationsAndChatService();
        //pinService?.SetupReceiveMessages();
    }
}
