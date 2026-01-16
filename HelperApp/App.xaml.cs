using HelperApp.Services;
using HelperApp.Views;

namespace HelperApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
