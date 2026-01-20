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
/// Mock сервис для разработки и тестирования без API
/// Возвращает моковые данные (InventoryTaskItem) для демонстрации
/// </summary>
public class MockTaskService : ITaskService
{
    private readonly ILogger<MockTaskService> _logger;
    private CancellationTokenSource? _syncCts;
    private Task? _syncTask;

    public MockTaskService(ILogger<MockTaskService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<TaskItemBase>> GetTasksForCurrentUserAsync(int employeeId)
    {
        // Имитируем задержку сети
        await Task.Delay(500);

        var mockTasks = new List<TaskItemBase>
        {
            // Задача 1: Инвентаризация зоны A (в процессе)
            new InventoryTaskItem
            {
                TaskId = 1,
                Type = TaskType.Inventory,
                BranchId = 1,
                Title = "Инвентаризация: зона A",
                Description = "Филиал: 1. Назначение: 101. Позиций: 3.",
                Status = TaskStatus.InProgress,
                Priority = 8,
                CreatedAt = DateTime.Now.AddDays(-2),
                CompletedAt = null,
                AssignedToEmployeeId = employeeId,
                AssignedAt = DateTime.Now.AddDays(-2),

                AssignmentId = 101,
                AssignmentStatus = InventoryAssignmentStatus.InProgress,
                ZoneCode = "A",
                Lines = new List<InventoryLineItem>
                {
                    new() { LineId = 1, ItemPositionId = 10, ExpectedQuantity = 5, ActualQuantity = 5, ZoneCode = "A", FirstLevelStorageType = "Rack", FlsNumber = "A-01" },
                    new() { LineId = 2, ItemPositionId = 11, ExpectedQuantity = 10, ActualQuantity = null, ZoneCode = "A", FirstLevelStorageType = "Rack", FlsNumber = "A-02" },
                    new() { LineId = 3, ItemPositionId = 12, ExpectedQuantity = 3, ActualQuantity = 4, ZoneCode = "A", FirstLevelStorageType = "Rack", FlsNumber = "A-03" }
                }
            },

            // Задача 2: Инвентаризация зоны B (назначена)
            new InventoryTaskItem
            {
                TaskId = 2,
                Type = TaskType.Inventory,
                BranchId = 1,
                Title = "Инвентаризация: зона B",
                Description = "Филиал: 1. Назначение: 102. Позиций: 2.",
                Status = TaskStatus.Assigned,
                Priority = 6,
                CreatedAt = DateTime.Now.AddDays(-1),
                CompletedAt = null,
                AssignedToEmployeeId = employeeId,
                AssignedAt = DateTime.Now.AddDays(-1),

                AssignmentId = 102,
                AssignmentStatus = InventoryAssignmentStatus.Assigned,
                ZoneCode = "B",
                Lines = new List<InventoryLineItem>
                {
                    new() { LineId = 4, ItemPositionId = 20, ExpectedQuantity = 8, ActualQuantity = null, ZoneCode = "B", FirstLevelStorageType = "Shelf", FlsNumber = "B-01" },
                    new() { LineId = 5, ItemPositionId = 21, ExpectedQuantity = 12, ActualQuantity = null, ZoneCode = "B", FirstLevelStorageType = "Shelf", FlsNumber = "B-02" }
                }
            },

            // Задача 3: Инвентаризация зоны C (завершена)
            new InventoryTaskItem
            {
                TaskId = 3,
                Type = TaskType.Inventory,
                BranchId = 2,
                Title = "Инвентаризация: зона C",
                Description = "Филиал: 2. Назначение: 103. Позиций: 4.",
                Status = TaskStatus.Completed,
                Priority = 5,
                CreatedAt = DateTime.Now.AddDays(-5),
                CompletedAt = DateTime.Now.AddDays(-1),
                AssignedToEmployeeId = employeeId,
                AssignedAt = DateTime.Now.AddDays(-5),

                AssignmentId = 103,
                AssignmentStatus = InventoryAssignmentStatus.Completed,
                ZoneCode = "C",
                Lines = new List<InventoryLineItem>
                {
                    new() { LineId = 6, ItemPositionId = 30, ExpectedQuantity = 15, ActualQuantity = 15, ZoneCode = "C", FirstLevelStorageType = "Bin", FlsNumber = "C-01" },
                    new() { LineId = 7, ItemPositionId = 31, ExpectedQuantity = 7, ActualQuantity = 7, ZoneCode = "C", FirstLevelStorageType = "Bin", FlsNumber = "C-02" },
                    new() { LineId = 8, ItemPositionId = 32, ExpectedQuantity = 20, ActualQuantity = 20, ZoneCode = "C", FirstLevelStorageType = "Bin", FlsNumber = "C-03" },
                    new() { LineId = 9, ItemPositionId = 33, ExpectedQuantity = 5, ActualQuantity = 5, ZoneCode = "C", FirstLevelStorageType = "Bin", FlsNumber = "C-04" }
                }
            }
        };

        _logger.LogInformation("Loaded {Count} mock tasks for employeeId={EmployeeId}",
            mockTasks.Count, employeeId);

        return mockTasks;
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
            _logger.LogInformation("Mock task sync started (interval: {IntervalSeconds}s)", intervalSeconds);

            while (!_syncCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), _syncCts.Token);
                    _logger.LogDebug("Mock: Checking for new tasks...");
                    // Mock не отправляет обновления
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Mock task sync cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during mock task sync");
                }
            }

        }, _syncCts.Token);
    }

    public void StopPeriodicSync()
    {
        if (_syncCts != null && !_syncCts.Token.IsCancellationRequested)
        {
            _syncCts.Cancel();
            _logger.LogInformation("Stop mock task sync requested");
        }
    }
}
