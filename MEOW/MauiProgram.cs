using MEOW.Components.Services;
using Microsoft.Extensions.Logging;

namespace MEOW;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

#if ANDROID
        builder.Services.AddSingleton<IBluetoothService, AndroidBluetoothService>();
#elif IOS
        builder.Services.AddSingleton<IBluetoothService, IOSBluetoothService>();
        builder.Services.AddSingleton<IChatService, ChatService>();
        builder.Services.AddTransient<INotificationManagerService, NotificationManagerService>();
#endif
        builder.Services.AddSingleton<IUserStateService, UserStateService>();

        builder.Services.AddSingleton<IMessageService, MessageService>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}