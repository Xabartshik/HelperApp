using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Models.Tasks;
using HelperApp.Services;
using System.Collections.ObjectModel;
using static HelperApp.Services.ApiClient;

namespace HelperApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ITaskService _taskService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IApiClient _apiClient;

    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private int employeeId;
    [ObservableProperty] private string role = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasNetwork = true;

    [ObservableProperty] private ObservableCollection<TaskItemBase> rawTasks = new();
    [ObservableProperty] private ObservableCollection<TaskCardVm> taskCards = new();

    [ObservableProperty] private TaskCardVm? selectedTaskCard;

    partial void OnSelectedTaskCardChanged(TaskCardVm? value)
    {
        if (value is null) return;

        OpenTaskCommand.Execute(value);

        // чтобы повторный тап по той же карточке снова срабатывал
        SelectedTaskCard = null;
    }

    private CancellationTokenSource? _cts;

    public MainViewModel(
        IAuthService authService,
        ITaskService taskService,
        ILogger<MainViewModel> logger,
        IApiClient apiClient)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
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
        FirstName = currentUser.FirstName;
        LastName = currentUser.LastName;
        FullName = currentUser.FullName;
        Role = currentUser.Role;

        await RefreshTasks();

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
                RawTasks.Clear();
                TaskCards.Clear();

                foreach (var task in tasks)
                {
                    RawTasks.Add(task);

                    // Пока у вас маппер используется только под inventory — оставляю как было
                    var card = TaskCardVm.TaskCardMapper.MapInventoryTaskToCard(task);
                    TaskCards.Add(card);
                }
            });

            HasNetwork = true;
        }
        catch (NoNetworkException)
        {
            ErrorMessage = "Нет подключения к сети";
            HasNetwork = false;
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
    public async Task OpenTask(TaskCardVm task)
    {
        if (task is null) return;

        try
        {
            switch (task.Kind)
            {
                case nameof(TaskType.Inventory):
                    await Shell.Current.GoToAsync(
                        "inventory-details",
                        new Dictionary<string, object>
                        {
                            ["assignmentId"] = task.NavigationId,
                            ["workerId"] = EmployeeId
                        });
                    break;

                default:
                    ErrorMessage = $"Не реализована навигация для типа задачи: {task.Kind}";
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка навигации: {ex.Message}";
            _logger.LogError(ex, "Ошибка при открытии задачи {TaskId}", task.NavigationId);
        }
    }

    [RelayCommand]
    public async Task Logout()
    {
        _cts?.Cancel();
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("///login");
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
                    RawTasks.Clear();
                    TaskCards.Clear();

                    foreach (var task in tasks)
                    {
                        RawTasks.Add(task);
                        var card = TaskCardVm.TaskCardMapper.MapInventoryTaskToCard(task);
                        TaskCards.Add(card);
                    }

                    HasNetwork = true;
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (NoNetworkException)
            {
                HasNetwork = false;
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

    public void StopPeriodicSync()
    {
        try
        {
            if (_cts is null) return;
            if (!_cts.IsCancellationRequested) _cts.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке синхронизации");
        }
    }
}
