using HelperApp.Services;
using HelperApp.ViewModels;
using HelperApp.Views;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace HelperApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            })
            // Добавляем ZXing
            .UseBarcodeReader()

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

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
