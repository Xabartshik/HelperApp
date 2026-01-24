using HelperApp.Services;
using HelperApp.ViewModels;
using HelperApp.Views;
using Microsoft.Extensions.Logging;

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

            // Сервисы
            .Services
            .AddSingleton<IApiClient, ApiClient>()
            .AddSingleton<IAuthService, AuthService>()

            // ViewModels
            .AddTransient<LoginViewModel>()
            .AddTransient<MainViewModel>()
            .AddTransient<InventoryDetailsViewModel>()


            // Views
            .AddTransient<LoginPage>()
            .AddTransient<MainPage>()
            .AddTransient<InventoryDetailsPage>()
            .AddSingleton<AppShell>();

        builder.Services.AddSingleton<ITaskService, TaskControlTaskService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
