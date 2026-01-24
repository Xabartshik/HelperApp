using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelperApp.Models.Inventory;
using HelperApp.Models.Tasks;
using Microsoft.Extensions.Logging;

namespace HelperApp.Services;

/// <summary>
/// Сервис управления задачами инвентаризации
/// Реализует интерфейс ITaskService с поддержкой новой структуры моделей (TaskItemBase/InventoryTaskItem)
/// </summary>
public class TaskControlTaskService : ITaskService, IDisposable
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<TaskControlTaskService> _logger;

    private Timer? _periodicSyncTimer;
    private Func<IEnumerable<InventoryTaskItem>, Task>? _onTasksUpdated;
    private int _lastSyncEmployeeId;

    public TaskControlTaskService(IApiClient apiClient, ILogger<TaskControlTaskService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Получить список задач для текущего пользователя
    /// </summary>
    public async Task<IEnumerable<InventoryTaskItem>> GetTasksForCurrentUserAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching tasks for employee {EmployeeId}", employeeId);

            if (employeeId <= 0)
            {
                _logger.LogWarning("Invalid employee ID provided: {EmployeeId}", employeeId);
                return Enumerable.Empty<InventoryTaskItem>();
            }

            var assignments = await _apiClient.GetNewAssignmentsForWorkerAsync(employeeId, cancellationToken);

            if (assignments == null || assignments.Count == 0)
            {
                _logger.LogInformation("No assignments found for employee {EmployeeId}", employeeId);
                return Enumerable.Empty<InventoryTaskItem>();
            }

            var tasks = assignments
                .Select(a => MapToInventoryTaskItem(a, employeeId))
                .Where(t => t != null)
                .ToList()!;

            _logger.LogInformation(
                "Successfully mapped {Count} tasks for employee {EmployeeId}",
                tasks.Count,
                employeeId);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tasks for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    /// <summary>
    /// Получить детальную информацию о назначении инвентаризации
    /// </summary>
    public async Task<InventoryTaskItem> GetInventoryTaskDetailsAsync(
        int employeeId,
        int inventoryTaskId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching details for inventory task {TaskId} for employee {EmployeeId}",
                inventoryTaskId,
                employeeId);

            if (employeeId <= 0 || inventoryTaskId <= 0)
            {
                _logger.LogWarning(
                    "Invalid parameters: EmployeeId={EmployeeId}, TaskId={TaskId}",
                    employeeId,
                    inventoryTaskId);

                throw new ArgumentException("Invalid employee or task ID");
            }

            var tasks = await GetTasksForCurrentUserAsync(employeeId, cancellationToken);

            // В новой модели навигация по AssignmentId
            var task = tasks.FirstOrDefault(t => t.AssignmentId == inventoryTaskId);

            if (task == null)
            {
                _logger.LogWarning(
                    "Inventory task {TaskId} not found for employee {EmployeeId}",
                    inventoryTaskId,
                    employeeId);

                throw new InvalidOperationException($"Inventory task {inventoryTaskId} not found");
            }

            _logger.LogInformation(
                "Successfully retrieved details for inventory task {TaskId}: {LineCount} lines",
                inventoryTaskId,
                task.Lines.Count);

            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory task details for task {TaskId}", inventoryTaskId);
            throw;
        }
    }

    /// <summary>
    /// Запустить периодическую синхронизацию задач
    /// </summary>
    public void StartPeriodicSync(Func<IEnumerable<InventoryTaskItem>, Task> onTasksUpdated, int intervalSeconds = 30)
    {
        try
        {
            if (onTasksUpdated == null)
                throw new ArgumentNullException(nameof(onTasksUpdated));

            if (intervalSeconds < 5)
            {
                _logger.LogWarning(
                    "Interval seconds is too small ({IntervalSeconds}). Using minimum of 5 seconds",
                    intervalSeconds);
                intervalSeconds = 5;
            }

            StopPeriodicSync();
            _onTasksUpdated = onTasksUpdated;

            _logger.LogInformation("Starting periodic sync with interval {IntervalSeconds} seconds", intervalSeconds);

            _periodicSyncTimer = new Timer(
                async _ => await PerformPeriodicSync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(intervalSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting periodic sync");
            throw;
        }
    }

    /// <summary>
    /// Остановить периодическую синхронизацию
    /// </summary>
    public void StopPeriodicSync()
    {
        try
        {
            if (_periodicSyncTimer != null)
            {
                _logger.LogInformation("Stopping periodic sync");
                _periodicSyncTimer.Dispose();
                _periodicSyncTimer = null;
            }

            _onTasksUpdated = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping periodic sync");
            throw;
        }
    }

    private async Task PerformPeriodicSync()
    {
        try
        {
            if (_lastSyncEmployeeId <= 0)
            {
                _logger.LogDebug("Skipping periodic sync: employee ID not set");
                return;
            }

            _logger.LogDebug("Performing periodic sync for employee {EmployeeId}", _lastSyncEmployeeId);

            var tasks = await GetTasksForCurrentUserAsync(_lastSyncEmployeeId);

            if (_onTasksUpdated != null)
                await _onTasksUpdated(tasks);

            _logger.LogDebug("Periodic sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic sync");
            // не пробрасываем, чтобы таймер продолжал работать
        }
    }

    /// <summary>
    /// Маппер DTO → InventoryTaskItem (новая модель)
    /// </summary>
    private InventoryTaskItem? MapToInventoryTaskItem(InventoryAssignmentDetailedWithItemDto assignment, int employeeId)
    {
        try
        {
            if (assignment == null)
            {
                _logger.LogWarning("Attempting to map null assignment");
                return null;
            }

            var createdAt = ParseCreatedDate(assignment.CreatedDate);

            return new InventoryTaskItem
            {
                // База
                TaskId = assignment.Id, // если появится отдельный taskId в DTO — заменить
                Type = TaskType.Inventory,
                BranchId = 0, // если появится BranchId в DTO — прокинуть
                Title = assignment.TaskNumber ?? $"Inventory {assignment.Id}",
                Description = assignment.Description,
                Status = TaskStatus.New, // если появится статус задачи — прокинуть
                Priority = 5,
                CreatedAt = createdAt,
                CompletedAt = null,
                AssignedToEmployeeId = employeeId,
                AssignedAt = createdAt,

                // Инвентаризация
                AssignmentId = assignment.Id,
                Lines = assignment.Lines != null
                    ? assignment.Lines
                        .Select(MapToInventoryLineItem)
                        .Where(l => l != null)
                        .ToList()!
                    : new List<InventoryLineItem>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping assignment {AssignmentId}", assignment?.Id);
            return null;
        }
    }

    /// <summary>
    /// Маппер DTO строки → InventoryLineItem (новая модель, PositionCode вместо ZoneCode/FirstLevelStorageType/FlsNumber)
    /// </summary>
    private InventoryLineItem? MapToInventoryLineItem(InventoryAssignmentLineWithItemDto line)
    {
        try
        {
            if (line == null)
            {
                _logger.LogWarning("Attempting to map null inventory line");
                return null;
            }

            return new InventoryLineItem
            {
                LineId = line.Id,
                ItemPositionId = line.ItemPositionId,
                ExpectedQuantity = line.ExpectedQuantity,
                ActualQuantity = line.ActualQuantity,

                PositionCode = line.PositionCode != null
                    ? MapToPositionCodeInfo(line.PositionCode)
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping inventory line {LineId}", line?.Id);
            return null;
        }
    }

    private PositionCodeInfo MapToPositionCodeInfo(PositionCodeDto positionCode)
    {
        // PositionCodeDto уже используется в проекте, просто переливаем в PositionCodeInfo
        return new PositionCodeInfo
        {
            BranchId = positionCode.BranchId,
            ZoneCode = positionCode.ZoneCode ?? "",
            FirstLevelStorageType = positionCode.FirstLevelStorageType ?? "",
            FLSNumber = positionCode.FLSNumber ?? "",
            SecondLevelStorage = positionCode.SecondLevelStorage,
            ThirdLevelStorage = positionCode.ThirdLevelStorage
        };
    }

    private DateTime ParseCreatedDate(string? dateString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                _logger.LogWarning("CreatedDate is null or empty, using current time");
                return DateTime.UtcNow;
            }

            if (DateTime.TryParse(dateString, out var date))
                return date;

            _logger.LogWarning("Failed to parse CreatedDate '{DateString}', using current time", dateString);
            return DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CreatedDate '{DateString}'", dateString);
            return DateTime.UtcNow;
        }
    }

    public bool IsPeriodicSyncActive => _periodicSyncTimer != null;

    public void SetEmployeeIdForPeriodicSync(int employeeId)
    {
        _lastSyncEmployeeId = employeeId;
        _logger.LogInformation("Employee ID for periodic sync set to {EmployeeId}", employeeId);
    }

    public int GetEmployeeIdForPeriodicSync => _lastSyncEmployeeId;

    public void Dispose()
    {
        StopPeriodicSync();
        _logger.LogInformation("TaskControlTaskService disposed");
    }
}
