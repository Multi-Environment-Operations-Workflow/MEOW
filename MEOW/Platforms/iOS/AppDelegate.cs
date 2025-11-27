using Foundation;
using MEOW_BUSINESS.Services;
using UIKit;

namespace MEOW;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        var chatService = IPlatformApplication.Current?.Services.GetService<IChatService>();
        var pinService = IPlatformApplication.Current?.Services.GetService<IPinService>();
        
        if (chatService == null || pinService == null)
            throw new Exception("Services not found in AppDelegate OnActivated");

        chatService.SetupNotificationsAndChatService();
        pinService.SetupReceiveMessages();
    }
}
