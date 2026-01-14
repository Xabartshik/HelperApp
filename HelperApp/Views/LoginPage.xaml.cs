using HelperApp.Models;


namespace HelperApp.Views;


public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }


    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var user = await App.ApiService.LoginAsync(
        UsernameEntry.Text!,
        PasswordEntry.Text!);


        if (user == null || !user.IsActive)
        {
            ErrorLabel.Text = "Неверные учетные данные";
            return;
        }


        await Navigation.PushAsync(new MainPage(user));
    }
}