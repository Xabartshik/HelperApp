using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Services;
using static HelperApp.Services.ApiClient;

namespace HelperApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginViewModel> _logger;
    private readonly IApiClient _apiClient;

    [ObservableProperty]
    private string employeeId = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasNetwork = true;

    public LoginViewModel(IAuthService authService, ILogger<LoginViewModel> logger, IApiClient apiClient)
    {
        _authService = authService;
        _logger = logger;
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task Login()
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(EmployeeId) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            return;
        }

        if (!int.TryParse(EmployeeId, out _))
        {
            ErrorMessage = "EmployeeId должен быть числом";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var currentUser = await _authService.LoginAsync(EmployeeId, Password);

            if (currentUser != null)
            {
                _logger.LogInformation("Переход на MainPage");
                await Shell.Current.GoToAsync("///main");
            }
        }
        catch (UnauthorizedException)
        {
            ErrorMessage = "Неверные учетные данные";
            _logger.LogWarning("Неверные учетные данные для пользователя {EmployeeId}", EmployeeId);
        }
        catch (NoNetworkException)
        {
            ErrorMessage = "Нет подключения к сети";
            HasNetwork = false;
            _logger.LogError("Нет подключения к сети при логине");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
            _logger.LogError(ex, "Ошибка при логине");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
