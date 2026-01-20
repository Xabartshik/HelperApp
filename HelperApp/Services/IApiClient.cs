using HelperApp.Models.Inventory;
using System.Text.Json;

namespace HelperApp.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, HttpContent content, CancellationToken ct = default);
    void SetAuthorizationToken(string? token);
    bool HasNetwork { get; }

    Task<bool> HasNewTasksForWorkerAsync(int workerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryAssignmentDetailedDto>?> GetNewAssignmentsForWorkerAsync(int workerId, CancellationToken cancellationToken = default);
}
