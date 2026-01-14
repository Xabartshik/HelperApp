using HelperApp.Models;


namespace HelperApp.Views;


public partial class MainPage : ContentPage
{
    private readonly MobileAppUserDto _user;


    public MainPage(MobileAppUserDto user)
    {
        InitializeComponent();
        _user = user;
        WelcomeLabel.Text = $"Добро пожаловать, {_user.EmployeeId} ({_user.Role})";
    }


    private async void OnProtectedClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProtectedPage(_user));
    }


    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}