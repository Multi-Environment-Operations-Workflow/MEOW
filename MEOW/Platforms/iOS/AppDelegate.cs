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
        base.OnActivated(application);
        var chatService = IPlatformApplication.Current?.Services.GetService<IChatService>();
        
        chatService?.SetupNotificationsAndChatService();
    }
}