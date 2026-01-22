using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelperApp.Models.Inventory;
using HelperApp.Models.Tasks;
using Microsoft.Extensions.Logging;

namespace HelperApp.Services
{
    /// <summary>
    /// Сервис управления задачами инвентаризации
    /// Реализует интерфейс ITaskService с поддержкой новой структуры DTO
    /// </summary>
    public class TaskControlTaskService : ITaskService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<TaskControlTaskService> _logger;
        private Timer _periodicSyncTimer;
        private Func<IEnumerable<InventoryTaskItem>, Task> _onTasksUpdated;
        private int _lastSyncEmployeeId;

        public TaskControlTaskService(
            IApiClient apiClient,
            ILogger<TaskControlTaskService> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Получить список задач для текущего пользователя
        /// Возвращает сырые задачи (для выполнения и детальной информации)
        /// </summary>
        public async Task<IEnumerable<InventoryTaskItem>> GetTasksForCurrentUserAsync(
            int employeeId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching tasks for employee {EmployeeId}", employeeId);

                if (employeeId <= 0)
                {
                    _logger.LogWarning(
                        "Invalid employee ID provided: {EmployeeId}", employeeId);
                    return Enumerable.Empty<InventoryTaskItem>();
                }

                var assignments = await _apiClient.GetNewAssignmentsForWorkerAsync(
                    employeeId,
                    cancellationToken);

                if (assignments == null || assignments.Count == 0)
                {
                    _logger.LogInformation(
                        "No assignments found for employee {EmployeeId}", employeeId);
                    return Enumerable.Empty<InventoryTaskItem>();
                }

                var tasks = assignments
                    .Select(a => MapToInventoryTaskItem(a))
                    .Where(t => t != null)
                    .ToList();

                _logger.LogInformation(
                    "Successfully mapped {Count} tasks for employee {EmployeeId}",
                    tasks.Count,
                    employeeId);

                return tasks; // Возвращаем как IEnumerable<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching tasks for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        /// <summary>
        /// Получить детальную информацию о назначении инвентаризации
        /// Включает данные о товарах, позициях, PositionCode
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

                // Получить все задачи сотрудника
                var tasks = await GetTasksForCurrentUserAsync(employeeId, cancellationToken);

                // Найти нужную задачу
                var task = tasks.FirstOrDefault(t => t.Id == inventoryTaskId);

                if (task == null)
                {
                    _logger.LogWarning(
                        "Inventory task {TaskId} not found for employee {EmployeeId}",
                        inventoryTaskId,
                        employeeId);
                    throw new InvalidOperationException(
                        $"Inventory task {inventoryTaskId} not found");
                }

                _logger.LogInformation(
                    "Successfully retrieved details for inventory task {TaskId}: " +
                    "{LineCount} lines, {ProcessedCount} processed",
                    inventoryTaskId,
                    task.Lines.Count,
                    task.ProcessedCount);

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching inventory task details for task {TaskId}",
                    inventoryTaskId);
                throw;
            }
        }

        /// <summary>
        /// Запустить периодическую синхронизацию задач
        /// Вызывает callback при обновлении списка задач
        /// </summary>
        public void StartPeriodicSync(
            Func<IEnumerable<InventoryTaskItem>, Task> onTasksUpdated,
            int intervalSeconds = 30)
        {
            try
            {
                if (onTasksUpdated == null)
                {
                    _logger.LogError("onTasksUpdated callback cannot be null");
                    throw new ArgumentNullException(nameof(onTasksUpdated));
                }

                if (intervalSeconds < 5)
                {
                    _logger.LogWarning(
                        "Interval seconds is too small ({IntervalSeconds}). " +
                        "Using minimum of 5 seconds",
                        intervalSeconds);
                    intervalSeconds = 5;
                }

                // Остановить существующий таймер если есть
                StopPeriodicSync();

                _onTasksUpdated = onTasksUpdated;

                _logger.LogInformation(
                    "Starting periodic sync with interval {IntervalSeconds} seconds",
                    intervalSeconds);

                // Создать таймер для периодической синхронизации
                _periodicSyncTimer = new Timer(
                    async _ => await PerformPeriodicSync(),
                    null,
                    TimeSpan.Zero, // Запустить сразу
                    TimeSpan.FromSeconds(intervalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error starting periodic sync");
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
                    _onTasksUpdated = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error stopping periodic sync");
                throw;
            }
        }

        /// <summary>
        /// Выполнить периодическую синхронизацию (внутренний метод)
        /// </summary>
        private async Task PerformPeriodicSync()
        {
            try
            {
                if (_lastSyncEmployeeId <= 0)
                {
                    _logger.LogDebug("Skipping periodic sync: employee ID not set");
                    return;
                }

                _logger.LogDebug(
                    "Performing periodic sync for employee {EmployeeId}",
                    _lastSyncEmployeeId);

                var tasks = await GetTasksForCurrentUserAsync(_lastSyncEmployeeId);

                if (_onTasksUpdated != null)
                {
                    await _onTasksUpdated(tasks);
                    _logger.LogDebug(
                        "Periodic sync completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error during periodic sync");
                // Не пробрасываем исключение, чтобы таймер продолжал работать
            }
        }


        /// <summary>
        /// Маппер для преобразования DTO в доменную модель InventoryTaskItem
        /// </summary>
        private InventoryTaskItem MapToInventoryTaskItem(
            InventoryAssignmentDetailedWithItemDto assignment)
        {
            try
            {
                _logger.LogDebug(
                    "Mapping assignment {AssignmentId} ({TaskNumber}) with {LineCount} lines",
                    assignment.Id,
                    assignment.TaskNumber,
                    assignment.Lines?.Count ?? 0);

                if (assignment == null)
                {
                    _logger.LogWarning("Attempting to map null assignment");
                    return null;
                }

                return new InventoryTaskItem
                {
                    Id = assignment.Id,
                    TaskNumber = assignment.TaskNumber,
                    Description = assignment.Description,
                    CreatedDate = ParseCreatedDate(assignment.CreatedDate),
                    Lines = assignment.Lines != null
                        ? assignment.Lines
                            .Select(l => MapToInventoryLineItem(l))
                            .Where(l => l != null)
                            .ToList()
                        : new List<InventoryLineItem>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error mapping assignment {AssignmentId}",
                    assignment?.Id);
                return null;
            }
        }

        /// <summary>
        /// Маппер для строки инвентаризации
        /// Маппирует новые поля товара и расположения ✨
        /// ИСПРАВЛЕНО: Принимает InventoryAssignmentLineWithItemDto (правильный тип)
        /// </summary>
        private InventoryLineItem MapToInventoryLineItem(
            InventoryAssignmentLineWithItemDto line)
        {
            try
            {
                _logger.LogDebug(
                    "Mapping inventory line {LineId}: ItemId={ItemId}, ItemName={ItemName}",
                    line.Id,
                    line.ItemId,
                    line.ItemName ?? "N/A");

                if (line == null)
                {
                    _logger.LogWarning("Attempting to map null inventory line");
                    return null;
                }

                return new InventoryLineItem
                {
                    Id = line.Id,
                    ItemPositionId = line.ItemPositionId,
                    PositionId = line.PositionId,
                    ExpectedQuantity = line.ExpectedQuantity,
                    ActualQuantity = line.ActualQuantity,

                    // ✨ NEW: Информация о товаре
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,

                    // ✨ NEW: Структурированная информация о расположении
                    PositionCode = line.PositionCode != null
                        ? MapToPositionCodeInfo(line.PositionCode)
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error mapping inventory line {LineId}",
                    line?.Id);
                return null;
            }
        }

        /// <summary>
        /// Маппер для информации о расположении
        /// ✨ NEW: Работает с новой структурой PositionCode (BranchId, FLSNumber как string, etc.)
        /// </summary>
        private PositionCodeInfo MapToPositionCodeInfo(PositionCodeDto positionCode)
        {
            try
            {
                _logger.LogDebug(
                    "Mapping position code: Branch={BranchId}, Zone={ZoneCode}, " +
                    "Type={Type}, FLS={FLS}, SecondLevel={SecondLevel}, ThirdLevel={ThirdLevel}",
                    positionCode.BranchId,
                    positionCode.ZoneCode,
                    positionCode.FirstLevelStorageType,
                    positionCode.FLSNumber,
                    positionCode.SecondLevelStorage ?? "N/A",
                    positionCode.ThirdLevelStorage ?? "N/A");

                if (positionCode == null)
                {
                    _logger.LogWarning("Attempting to map null position code");
                    return null;
                }

                return new PositionCodeInfo
                {
                    BranchId = positionCode.BranchId,
                    ZoneCode = positionCode.ZoneCode,
                    FirstLevelStorageType = positionCode.FirstLevelStorageType,
                    FLSNumber = positionCode.FLSNumber,
                    SecondLevelStorage = positionCode.SecondLevelStorage,
                    ThirdLevelStorage = positionCode.ThirdLevelStorage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error mapping position code");
                return null;
            }
        }

        /// <summary>
        /// Парсинг даты создания задачи
        /// Поддерживает различные форматы дат
        /// </summary>
        private DateTime ParseCreatedDate(string dateString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dateString))
                {
                    _logger.LogWarning("CreatedDate is null or empty, using current time");
                    return DateTime.UtcNow;
                }

                if (DateTime.TryParse(dateString, out var date))
                {
                    return date;
                }

                _logger.LogWarning(
                    "Failed to parse CreatedDate '{DateString}', using current time",
                    dateString);
                return DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error parsing CreatedDate '{DateString}'",
                    dateString);
                return DateTime.UtcNow;
            }
        }

        #region Методы для отладки

        /// <summary>
        /// Получить статус периодической синхронизации
        /// </summary>
        public bool IsPeriodicSyncActive => _periodicSyncTimer != null;

        /// <summary>
        /// Установить последний ID сотрудника для периодической синхронизации
        /// </summary>
        public void SetEmployeeIdForPeriodicSync(int employeeId)
        {
            _lastSyncEmployeeId = employeeId;
            _logger.LogInformation(
                "Employee ID for periodic sync set to {EmployeeId}",
                employeeId);
        }

        /// <summary>
        /// Получить последний ID сотрудника для периодической синхронизации
        /// </summary>
        public int GetEmployeeIdForPeriodicSync => _lastSyncEmployeeId;

        #endregion

        /// <summary>
        /// Очистка ресурсов
        /// </summary>
        public void Dispose()
        {
            StopPeriodicSync();
            _logger.LogInformation("TaskControlTaskService disposed");
        }
    }
}
