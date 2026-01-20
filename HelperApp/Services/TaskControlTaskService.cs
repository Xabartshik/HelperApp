
using HelperApp.Models.Inventory;
using HelperApp.Models.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HelperApp.Services;

/// <summary>
/// Реальный сервис для получения задач из TaskControl API
/// Преобразует DTO с сервера в структурированные модели (TaskItemBase)
/// </summary>
public sealed class TaskControlTaskService : ITaskService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<TaskControlTaskService> _logger;

    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;
    private int? _lastEmployeeId;

    public TaskControlTaskService(IApiClient apiClient, ILogger<TaskControlTaskService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<TaskItemBase>> GetTasksForCurrentUserAsync(int employeeId)
    {
        _lastEmployeeId = employeeId;

        try
        {
            // Получаем назначения инвентаризации с сервера
            var assignments = await _apiClient.GetNewAssignmentsForWorkerAsync(employeeId);

            if (assignments is null || assignments.Count == 0)
            {
                _logger.LogInformation("No assignments returned for employeeId={EmployeeId}", employeeId);
                return Enumerable.Empty<TaskItemBase>();
            }

            // Преобразуем DTO в структурированные модели
            var tasks = assignments.Select(a => MapToInventoryTaskItem(a, employeeId)).ToList();

            _logger.LogInformation("Loaded {Count} tasks for employeeId={EmployeeId}", tasks.Count, employeeId);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tasks for employeeId={EmployeeId}", employeeId);
            return Enumerable.Empty<TaskItemBase>();
        }
    }

    public void StartPeriodicSync(Func<IEnumerable<TaskItemBase>, Task> onTasksUpdated, int intervalSeconds = 30)
    {
        if (onTasksUpdated is null)
            throw new ArgumentNullException(nameof(onTasksUpdated));

        if (_syncTask != null && !_syncTask.IsCompleted)
        {
            _logger.LogWarning("Task sync is already running");
            return;
        }

        _syncCts = new CancellationTokenSource();
        _syncTask = Task.Run(async () =>
        {
            _logger.LogInformation("Task sync started (interval: {IntervalSeconds}s)", intervalSeconds);

            while (!_syncCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), _syncCts.Token);

                    if (_lastEmployeeId is null)
                        continue;

                    var hasNew = await _apiClient.HasNewTasksForWorkerAsync(_lastEmployeeId.Value, _syncCts.Token);

                    if (!hasNew)
                        continue;

                    var updated = await GetTasksForCurrentUserAsync(_lastEmployeeId.Value);
                    await onTasksUpdated(updated);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Task sync cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during task sync");
                }
            }

        }, _syncCts.Token);
    }

    public void StopPeriodicSync()
    {
        if (_syncCts != null && !_syncCts.Token.IsCancellationRequested)
        {
            _syncCts.Cancel();
            _logger.LogInformation("Stop task sync requested");
        }
    }

    /// <summary>
    /// Маппер: InventoryAssignmentDetailedDto → InventoryTaskItem
    /// </summary>
    private static InventoryTaskItem MapToInventoryTaskItem(
        InventoryAssignmentDetailedDto dto, int employeeId)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var lines = dto.Lines?.Select(l => new InventoryLineItem
        {
            LineId = l.Id,
            ItemPositionId = l.ItemPositionId,
            ExpectedQuantity = l.ExpectedQuantity,
            ActualQuantity = l.ActualQuantity,
            ZoneCode = l.ZoneCode ?? "",
            FirstLevelStorageType = l.FirstLevelStorageType ?? "",
            FlsNumber = l.FlsNumber?.ToString() ?? ""
        }).ToList() ?? new List<InventoryLineItem>();

        return new InventoryTaskItem
        {
            // Базовые поля (из TaskItemBase)
            TaskId = dto.TaskId,
            Type = TaskType.Inventory,
            BranchId = dto.BranchId,
            Title = !string.IsNullOrWhiteSpace(dto.ZoneCode)
                ? $"Инвентаризация (зона {dto.ZoneCode})"
                : $"Инвентаризация #{dto.TaskId}",
            Description = $"Филиал: {dto.BranchId}. Назначение: {dto.Id}. Позиций: {lines.Count}.",
            Status = MapStatus(dto.Status),
            Priority = CalculatePriority(dto.Status),
            CreatedAt = dto.AssignedAt,
            CompletedAt = dto.CompletedAt,
            AssignedToEmployeeId = employeeId,
            AssignedAt = dto.AssignedAt,

            // Специальные поля инвентаризации
            AssignmentId = dto.Id,
            AssignmentStatus = dto.Status,
            ZoneCode = dto.ZoneCode,
            Lines = lines
        };
    }

    /// <summary>
    /// Преобразование статуса из DTO в TaskStatus
    /// </summary>
    private static TaskStatus MapStatus(InventoryAssignmentStatus dtoStatus)
    {
        return dtoStatus switch
        {
            InventoryAssignmentStatus.Assigned => TaskStatus.Assigned,
            InventoryAssignmentStatus.InProgress => TaskStatus.InProgress,
            InventoryAssignmentStatus.Completed => TaskStatus.Completed,
            InventoryAssignmentStatus.Cancelled => TaskStatus.Cancelled,
            _ => TaskStatus.New
        };
    }

    /// <summary>
    /// Расчёт приоритета на основе статуса
    /// </summary>
    private static int CalculatePriority(InventoryAssignmentStatus status)
    {
        return status switch
        {
            InventoryAssignmentStatus.InProgress => 8,
            InventoryAssignmentStatus.Assigned => 6,
            InventoryAssignmentStatus.Completed => 3,
            InventoryAssignmentStatus.Cancelled => 1,
            _ => 5
        };
    }
}
