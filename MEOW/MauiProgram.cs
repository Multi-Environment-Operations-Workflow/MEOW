using MatBlazor;
using MEOW_BUSINESS.Services;
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
        builder.Services.AddSingleton<INotificationManagerService, AndroidNotificationManagerService>();
#elif IOS
        builder.Services.AddSingleton<IBluetoothService, IOSBluetoothService>();
        builder.Services.AddSingleton<INotificationManagerService, IOSNotificationManagerService>();
#endif
        builder.Services.AddSingleton<IUserStateService, UserStateService>();

        builder.Services.AddSingleton<INavService, NavService>();
        builder.Services.AddSingleton<IQuickChatService, QuickChatService>();
        builder.Services.AddSingleton<IPinService, PinService>();
        builder.Services.AddSingleton<IMessageService, MessageService>();
        builder.Services.AddSingleton<IChatService, ChatService>();
        builder.Services.AddSingleton<IErrorService, MeowErrorService>();
        builder.Services.AddSingleton<IMeowPreferences, MeowPreferences>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMatBlazor();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}