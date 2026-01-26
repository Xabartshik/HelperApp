using HelperApp.Services;
using HelperApp.Views;

namespace HelperApp;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell()
    {
        InitializeComponent();

        _authService = IPlatformApplication.Current!.Services.GetService<IAuthService>()!;

        // ВАЖНО: details-страницы регистрируем как routes (а не ShellContent)
        Routing.RegisterRoute("inventory-details", typeof(InventoryDetailsPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var currentUser = await _authService.TryAutoLoginAsync();

        if (currentUser != null)
            await GoToAsync("///main");
        else
            await GoToAsync("///login");
    }
}
