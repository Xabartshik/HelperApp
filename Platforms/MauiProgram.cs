using Microsoft.Extensions.Logging;
using HelperApp.Services;
using HelperApp.Handlers;

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
            });

        // Логирование: вывод в Debug (Visual Studio Output) и включаем Trace для сетевых категорий
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.Logging.AddFilter("System.Net.Http", LogLevel.Trace);
        builder.Logging.AddFilter("System.Net.Http.SocketsHttpHandler", LogLevel.Trace);
        builder.Logging.AddFilter("System.Net.NameResolution", LogLevel.Trace);
        builder.Logging.AddFilter("System.Net.Security", LogLevel.Trace);

        // Регистрируем делегирующий обработчик и HttpClient для ApiService через IHttpClientFactory
        builder.Services.AddTransient<LoggingHandler>();
        builder.Services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri("http://10.0.2.2:5000/");
        })
        .AddHttpMessageHandler<LoggingHandler>();

        return builder.Build();
    }
}
