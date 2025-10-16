﻿using MEOW.Components.Services;
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
        // Register platform-specific Bluetooth service
#if ANDROID
        builder.Services.AddSingleton<IBluetoothService, AndroidBluetoothService>();
#elif IOS
        builder.Services.AddSingleton<IBluetoothService, IOSBluetoothService>();
#elif MACCATALYST
        builder.Services.AddSingleton<IBluetoothService, MacBluetoothService>();
#endif

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}