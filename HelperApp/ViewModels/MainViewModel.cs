using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Models.Tasks;
using HelperApp.Services;
using System.Collections.ObjectModel;

namespace HelperApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ITaskService _taskService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IApiClient _apiClient;

    [ObservableProperty]
    private int employeeId;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasNetwork = true;

    [ObservableProperty]
    private ObservableCollection<TaskItem> tasks = new();

    private CancellationTokenSource? _cts;

    public MainViewModel(
        IAuthService authService,
        ITaskService taskService,
        ILogger<MainViewModel> logger,
        IApiClient apiClient)
    {
        _authService = authService;
        _taskService = taskService;
        _logger = logger;
        _apiClient = apiClient;
    }

    public async Task InitializeAsync()
    {
        var currentUser = _authService.GetCurrentUser();

        if (currentUser == null)
        {
            _logger.LogWarning("CurrentUser не найден, перенаправление на логин");
            await Shell.Current.GoToAsync("///login");
            return;
        }

        EmployeeId = currentUser.EmployeeId;
        Role = currentUser.Role;

        _logger.LogInformation("MainViewModel инициализирована для EmployeeId={EmployeeId}", EmployeeId);

        // Загружаем задачи
        await RefreshTasks();

        // Запускаем периодическую синхронизацию
        _cts = new CancellationTokenSource();
        _ = StartPeriodicTaskSync(_cts.Token);
    }

    [RelayCommand]
    public async Task RefreshTasks()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var tasks = await _taskService.GetTasksForCurrentUserAsync(EmployeeId);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
            });

            HasNetwork = true;
        }
        catch (NoNetworkException)
        {
            ErrorMessage = "Нет подключения к сети";
            HasNetwork = false;
            _logger.LogError("Нет подключения при загрузке задач");
        }
        catch (UnauthorizedException)
        {
            await Logout();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки задач: {ex.Message}";
            _logger.LogError(ex, "Ошибка при загрузке задач");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task Logout()
    {
        _cts?.Cancel();
        await _authService.LogoutAsync();
        _logger.LogInformation("Выход из приложения");
        await Shell.Current.GoToAsync("login");
    }

    private async Task StartPeriodicTaskSync(CancellationToken cancellationToken)
    {
        const int intervalSeconds = 30;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);

                var tasks = await _taskService.GetTasksForCurrentUserAsync(EmployeeId);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Tasks.Clear();
                    foreach (var task in tasks)
                    {
                        Tasks.Add(task);
                    }
                    HasNetwork = true;
                });

                _logger.LogDebug("Задачи синхронизированы в {Time}", DateTime.Now);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Синхронизация задач отменена");
                break;
            }
            catch (NoNetworkException)
            {
                HasNetwork = false;
                _logger.LogWarning("Нет сети при синхронизации задач");
            }
            catch (UnauthorizedException)
            {
                await Logout();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при синхронизации задач");
            }
        }
    }
}
