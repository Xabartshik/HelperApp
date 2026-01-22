using HelperApp.Models.Inventory;
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
    Task<IEnumerable<InventoryTaskItem>> GetTasksForCurrentUserAsync(int employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить детальную информацию о назначении инвентаризации
    /// Включает данные о товарах, позициях, PositionCode
    /// </summary>
    Task<InventoryTaskItem?> GetInventoryTaskDetailsAsync(
        int employeeId,
        int inventoryTaskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Запустить периодическую синхронизацию задач
    /// </summary>
    void StartPeriodicSync(Func<IEnumerable<InventoryTaskItem>, Task> onTasksUpdated, int intervalSeconds = 30);

    /// <summary>
    /// Остановить периодическую синхронизацию
    /// </summary>
    void StopPeriodicSync();
}