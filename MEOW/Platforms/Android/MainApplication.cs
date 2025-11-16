using Android.App;
using Android.Runtime;
using MEOW_BUSINESS.Services;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Extensions.DependencyInjection;
using Exception = Java.Lang.Exception;

namespace MEOW;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate() //Changes here should be reflected in Platforms/iOS/AppDelegate.cs
    {
        base.OnCreate();
        var chatService = IPlatformApplication.Current?.Services.GetService<IChatService>();
        var pinService = IPlatformApplication.Current?.Services.GetService<IPinService>();

        chatService?.SetupNotificationsAndChatService();
        pinService?.SetupReceiveMessages();
    }
}
