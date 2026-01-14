using HelperApp.Models;


namespace HelperApp.Views;


public partial class ProtectedPage : ContentPage
{
    public ProtectedPage(MobileAppUserDto user)
    {
        InitializeComponent();
        InfoLabel.Text = $"Доступ разрешён пользователю {user.EmployeeId}";
    }
}