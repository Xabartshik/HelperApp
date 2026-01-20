using HelperApp.Models.Tasks;

namespace HelperApp.Services;

/// <summary>
/// Интерфейс сервиса управления задачами
/// Возвращает структурированные задачи (TaskItemBase и наследники)
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Получить список задач для текущего пользователя
    /// Возвращает сырые задачи (для выполнения и детальной информации)
    /// </summary>
    Task<IEnumerable<TaskItemBase>> GetTasksForCurrentUserAsync(int employeeId);

    /// <summary>
    /// Запустить периодическую синхронизацию задач
    /// </summary>
    void StartPeriodicSync(Func<IEnumerable<TaskItemBase>, Task> onTasksUpdated, int intervalSeconds = 30);

    /// <summary>
    /// Остановить периодическую синхронизацию
    /// </summary>
    void StopPeriodicSync();
}
