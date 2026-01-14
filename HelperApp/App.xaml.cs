using Microsoft.Maui.Controls;


namespace HelperApp;


public partial class App : Application
{
    public static Services.ApiService ApiService { get; private set; }


    public App()
    {
        InitializeComponent();
        ApiService = new Services.ApiService();
        MainPage = new NavigationPage(new Views.LoginPage());
    }
}