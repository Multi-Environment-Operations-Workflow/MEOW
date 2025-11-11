using Foundation;
using MEOW.Components.Services;
using UIKit;

namespace MEOW;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnActivated(UIApplication application)
    {
        base.OnCreate();
        var chatService = IPlatformApplication.Current?.Services.GetService<IChatService>();
        // I think this should work for IOS - Theis
        //var pinService = IPlatformApplication.Current?.Services.GetService<IPinService>();

        chatService?.SetupNotificationsAndChatService();
        //pinService?.SetupReceiveMessages();
    }
}
