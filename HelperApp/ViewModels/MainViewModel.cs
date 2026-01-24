using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Models.Inventory;
using HelperApp.Models.Tasks;
using HelperApp.Services;
using System.Collections.ObjectModel;
using static HelperApp.Services.ApiClient;

namespace HelperApp.ViewModels;

/// <summary>
/// ViewModel для главной страницы (списка задач)
/// Управляет загрузкой и отображением задач, синхронизацией с сервером, навигацией
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ITaskService _taskService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IApiClient _apiClient;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

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

    /// <summary>
    /// Сырые задачи (для выполнения и детальной информации)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TaskItemBase> rawTasks = new();

    /// <summary>
    /// Карточки задач для отображения (преобразованные из rawTasks)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TaskCardVm> taskCards = new();

    /// <summary>
    /// Выбранная карточка задачи (для навигации по клику)
    /// </summary>
    [ObservableProperty]
    private TaskCardVm? selectedTaskCard;

    partial void OnSelectedTaskCardChanged(TaskCardVm? value)
    {
        if (value is null) return;
        OpenTaskCommand.Execute(value);
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
                RawTasks.Clear();
                TaskCards.Clear();

                foreach (var task in tasks)
                {
                    // Сохраняем сырую задачу для выполнения
                    RawTasks.Add(task);

                    // Маппим в карточку для отображения
                    var card = TaskCardVm.TaskCardMapper.MapInventoryTaskToCard(task);
                    TaskCards.Add(card);
                }

                _logger.LogInformation("Loaded {Count} task cards for display", TaskCards.Count);
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

    /// <summary>
    /// Открыть страницу деталей задачи в зависимости от типа
    /// Использует встроенную навигацию Shell для расширяемости
    /// </summary>
    [RelayCommand]
    public async Task OpenTask(TaskCardVm task)
    {
        if (task == null) return;

        try
        {
            var route = task.Kind switch
            {
                nameof(TaskType.Inventory) => CreateInventoryDetailsRoute(task),
                nameof(TaskType.Receipt) => CreateReceiptDetailsRoute(task),
                nameof(TaskType.Movement) => CreateMovementDetailsRoute(task),
                nameof(TaskType.Shipping) => CreateShippingDetailsRoute(task),
                nameof(TaskType.Packing) => CreatePackingDetailsRoute(task),
                nameof(TaskType.Audit) => CreateAuditDetailsRoute(task),
                nameof(TaskType.Labeling) => CreateLabelingDetailsRoute(task),
                nameof(TaskType.Loading) => CreateLoadingDetailsRoute(task),
                _ => null
            };

            if (!string.IsNullOrEmpty(route))
            {
                await Shell.Current.GoToAsync(route);
            }
            else
            {
                ErrorMessage = $"Не реализована навигация для типа задачи: {task.Kind}";
                _logger.LogWarning("Навигация не реализована для типа задачи {Kind}", task.Kind);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка навигации: {ex.Message}";
            _logger.LogError(ex, "Ошибка при открытии задачи {TaskId}", task.NavigationId);
        }
    }

    /// <summary>
    /// Создать маршрут для страницы деталей инвентаризации
    /// </summary>
    private static string CreateInventoryDetailsRoute(TaskCardVm task) =>
        $"///inventory-details?assignmentId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей приёма
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreateReceiptDetailsRoute(TaskCardVm task) => 
        $"receipt-details?taskId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей перемещения
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreateMovementDetailsRoute(TaskCardVm task) => 
        $"movement-details?taskId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей отправки
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreateShippingDetailsRoute(TaskCardVm task) => 
        $"shipping-details?taskId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей упаковки
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreatePackingDetailsRoute(TaskCardVm task) => 
        $"packing-details?taskId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей аудита
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreateAuditDetailsRoute(TaskCardVm task) => 
        $"audit-details?taskId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей маркировки
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreateLabelingDetailsRoute(TaskCardVm task) => 
        $"labeling-details?taskId={task.NavigationId}";

    /// <summary>
    /// Создать маршрут для страницы деталей загрузки
    /// TODO: реализовать по мере необходимости
    /// </summary>
    private static string CreateLoadingDetailsRoute(TaskCardVm task) => 
        $"loading-details?taskId={task.NavigationId}";

    [RelayCommand]
    public async Task Logout()
    {
        _cts?.Cancel();
        await _authService.LogoutAsync();
        _logger.LogInformation("Выход из приложения");
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
                    _logger.LogDebug("Задачи синхронизированы в {Time}", DateTime.Now);
                });
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

    public void StopPeriodicSync()
    {
        try
        {
            if (_cts is null)
                return;

            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке синхронизации");
        }
    }
}
