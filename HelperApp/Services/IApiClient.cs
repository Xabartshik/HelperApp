using HelperApp.Models;
using HelperApp.Models.Inventory;

namespace HelperApp.Services;

public interface IApiClient
{
    void SetAuthorizationToken(string? token);
    bool HasNetwork { get; }

    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, HttpContent content, CancellationToken ct = default);

    // ===== Специфичные методы под задачи =====
    Task<bool> HasNewTasksForWorkerAsync(int workerId, CancellationToken cancellationToken = default);
    Task<List<InventoryAssignmentDetailedWithItemDto>?> GetNewAssignmentsForWorkerAsync(int workerId, CancellationToken cancellationToken = default);
    Task<InventoryTaskDetailsDto?> GetInventoryTaskDetailsAsync(int userId, int inventoryTaskId, CancellationToken cancellationToken = default);
    Task<ItemInfoDto?> GetItemInfoAsync(int itemId, CancellationToken cancellationToken = default);
}
