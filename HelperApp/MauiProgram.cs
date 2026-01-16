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
            .AddSingleton<ITaskService, MockTaskService>()

            // ViewModels
            .AddTransient<LoginViewModel>()
            .AddTransient<MainViewModel>()

            // Views
            .AddTransient<LoginPage>()
            .AddTransient<MainPage>()
            .AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
