using Foundation;
using MEOW.Components.Models;
using MEOW.Components.Services;
using UIKit;

namespace MEOW;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private IChatService? _chatService;
    private INotificationManagerService? _notificationManagerService;
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        try
        {
            var mauiApp = CreateMauiApp();
            _chatService = mauiApp.Services.GetService<IChatService>();
            _notificationManagerService = mauiApp.Services.GetService<INotificationManagerService>();

            if (_chatService == null || _notificationManagerService == null)
                return base.FinishedLaunching(application, launchOptions);

            void Action(MeowMessageText msg)
            {
                _notificationManagerService?.SendNotification("MEOW: Message", msg.Message,
                    DateTime.Now + TimeSpan.FromSeconds(5));
            }

            _chatService.SetupChatMessageReceivedAction(Action);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppDelegate FinishedLaunching Exception: {ex}");
        }
        return base.FinishedLaunching(application, launchOptions);
    }
    
    
}