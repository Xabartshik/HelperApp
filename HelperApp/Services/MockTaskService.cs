using HelperApp.Models.Tasks;

namespace HelperApp.Services;

public class MockTaskService : ITaskService
{
    private readonly ILogger<MockTaskService> _logger;
    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;

    public MockTaskService(ILogger<MockTaskService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TaskItem>> GetTasksForCurrentUserAsync(int employeeId)
    {
        // Заглушка: возвращаем моковые задачи
        await Task.Delay(500); // Имитируем задержку сети

        var mockTasks = new List<TaskItem>
        {
            new()
            {
                Id = 1,
                Title = "Завершить отчет",
                Description = "Подготовить еженедельный отчет о деятельности",
                Status = "New",
                CreatedAt = DateTime.Now.AddDays(-1),
                AssignedToEmployeeId = employeeId
            },
            new()
            {
                Id = 2,
                Title = "Провести встречу с командой",
                Description = "Обсудить план на следующий спринт",
                Status = "InProgress",
                CreatedAt = DateTime.Now.AddDays(-3),
                AssignedToEmployeeId = employeeId
            },
            new()
            {
                Id = 3,
                Title = "Обновить документацию",
                Description = "Актуализировать технологическую документацию",
                Status = "New",
                CreatedAt = DateTime.Now,
                AssignedToEmployeeId = employeeId
            }
        };

        _logger.LogInformation("Загруженно {Count} моковых задач для пользователя {EmployeeId}", 
            mockTasks.Count, employeeId);

        return mockTasks;
    }

    public void StartPeriodicSync(Func<IEnumerable<TaskItem>, Task> onTasksUpdated, int intervalSeconds = 30)
    {
        if (_syncTask != null && !_syncTask.IsCompleted)
        {
            _logger.LogWarning("Синхронизация уже запущена");
            return;
        }

        _syncCts = new CancellationTokenSource();

        _syncTask = Task.Run(async () =>
        {
            _logger.LogInformation("Синхронизация задач начата (интервал: {Interval}сек)", intervalSeconds);

            while (!_syncCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), _syncCts.Token);

                    // Здесь будет реальный вызов сервиса, пока что не делаем ничего
                    _logger.LogDebug("Проверка новых задач...");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Синхронизация задач остановлена");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при синхронизации задач");
                }
            }
        }, _syncCts.Token);
    }

    public void StopPeriodicSync()
    {
        if (_syncCts != null && !_syncCts.Token.IsCancellationRequested)
        {
            _syncCts.Cancel();
            _logger.LogInformation("Остановка синхронизации задач запрошена");
        }
    }
}
