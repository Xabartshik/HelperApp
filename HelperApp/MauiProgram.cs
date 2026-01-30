using HelperApp.Services;
using HelperApp.ViewModels;
using HelperApp.Views;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;
using ZXing.Net.Maui;
namespace HelperApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
#if DEBUG
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddDebug();
        builder.Logging.AddConsole(); // на Android это будет видно через logcat
#endif
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })

            // Сервисы
            .Services
            .AddSingleton<IApiClient, ApiClient>()
            .AddSingleton<IAuthService, AuthService>()

            // ViewModels
            .AddTransient<LoginViewModel>()
            .AddTransient<MainViewModel>()
            .AddTransient<InventoryDetailsViewModel>()
            .AddTransient<BarcodeScannerViewModel>()

            // Views
            .AddTransient<LoginPage>()
            .AddTransient<MainPage>()
            .AddTransient<InventoryDetailsPage>()
            .AddTransient<BarcodeScannerPage>()
            .AddSingleton<AppShell>();

        builder.Services.AddSingleton<ITaskService, TaskControlTaskService>();
        return builder.Build();
    }
}
