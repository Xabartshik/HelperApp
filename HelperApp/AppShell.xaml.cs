using HelperApp.Services;

namespace HelperApp;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell()
    {
        InitializeComponent();
        _authService = IPlatformApplication.Current!.Services.GetService<IAuthService>()!;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Пытаемся автоматически залогиниться
        var currentUser = await _authService.TryAutoLoginAsync();

        if (currentUser != null)
        {
            // Если есть валидный токен, переходим на MainPage
            await GoToAsync("///main");
        }
        else
        {
            // Иначе на LoginPage
            await GoToAsync("///login");
        }
    }
}
