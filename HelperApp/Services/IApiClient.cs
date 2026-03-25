using HelperApp.Models.Inventory;
using HelperApp.Models.BossPanel;
using HelperApp.Models;

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
    Task<CompleteAssignmentResultDto?> CompleteInventoryAssignmentAsync(CompleteAssignmentDto dto, CancellationToken cancellationToken = default);

    // ===== Boss Panel =====
    Task<List<BossPanelTaskCardDto>?> GetBossPanelActiveTasksAsync(CancellationToken cancellationToken = default);
    Task<List<EmployeeWorkloadDto>?> GetBossPanelEmployeeWorkloadAsync(CancellationToken cancellationToken = default);
    Task<List<AvailableEmployeeDto>?> GetBossPanelAvailableEmployeesAsync(CancellationToken cancellationToken = default);
    Task<List<int>?> GetBossPanelAutoSelectedEmployeesAsync(int count, CancellationToken cancellationToken = default);
    Task<CompleteInventoryDto?> CreateBossPanelInventoryTaskAsync(CreateInventoryTaskDto dto, CancellationToken cancellationToken = default);
    Task<List<string>?> GetBossPanelAvailableZonesAsync(CancellationToken cancellationToken = default);
    Task<List<PositionCellDto>?> GetBossPanelPositionsAsync(CancellationToken cancellationToken = default);
    Task<CompleteInventoryDto?> CreateBossPanelInventoryTaskByZoneAsync(CreateInventoryByZoneDto dto, CancellationToken cancellationToken = default);
}
