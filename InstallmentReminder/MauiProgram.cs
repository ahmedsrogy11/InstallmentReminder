using Microsoft.Extensions.Logging;
using InstallmentReminder.Data;
using InstallmentReminder.Services;
using InstallmentReminder.Views;

#if ANDROID || IOS
using Plugin.LocalNotification;
#endif

namespace InstallmentReminder;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

#if ANDROID || IOS
        builder.UseLocalNotification();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<InstallmentDatabase>();
        builder.Services.AddSingleton<ReminderService>();
        builder.Services.AddSingleton<InvoiceImageService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<InstallmentsPage>();

        return builder.Build();
    }
}
