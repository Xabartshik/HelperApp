using System;
using System.Collections.Generic;
using HelperApp.Models.Inventory;
using HelperApp.Services;

namespace HelperApp.Models.Tasks;

/// <summary>
/// Человекочитаемый номер/адрес складской позиции
/// </summary>
public class PositionCodeInfo
{
    /// <summary>
    /// Идентификатор филиала.
    /// </summary>
    public int BranchId { get; set; }

    /// <summary>
    /// Код зоны хранения.
    /// </summary>
    public string ZoneCode { get; set; } = "";

    /// <summary>
    /// Тип хранилища первого уровня (стеллаж, пол, ячейка и т.п.).
    /// </summary>
    public string FirstLevelStorageType { get; set; } = "";

    /// <summary>
    /// Номер хранилища первого уровня.
    /// </summary>
    public string FLSNumber { get; set; } = "";

    /// <summary>
    /// Номер хранилища второго уровня (опционально).
    /// </summary>
    public string? SecondLevelStorage { get; set; }

    /// <summary>
    /// Номер хранилища третьего уровня (опционально).
    /// </summary>
    public string? ThirdLevelStorage { get; set; }

    /// <summary>
    /// Человекочитаемое представление позиции.
    /// Пример: "1-ZA-RACK-A1-S1-C3"
    /// </summary>
    public string FullDescription =>
        $"{BranchId}-{ZoneCode}-{FirstLevelStorageType}-{FLSNumber}" +
        (!string.IsNullOrEmpty(SecondLevelStorage) ? $"-{SecondLevelStorage}" : string.Empty) +
        (!string.IsNullOrEmpty(ThirdLevelStorage) ? $"-{ThirdLevelStorage}" : string.Empty);

    /// <summary>
    /// Краткое описание (для отображения в UI).
    /// Пример: "ZA-RACK-A1"
    /// </summary>
    public string ShortDescription =>
        $"{ZoneCode}-{FirstLevelStorageType}-{FLSNumber}";
}

/// <summary>
/// Перечисление типов задач
/// </summary>
public enum TaskType
{
    Inventory,
    Receipt,
    Movement,
    Shipping,
    Packing,
    Audit,
    Labeling,
    Loading
}

/// <summary>
/// Перечисление статусов задач
/// </summary>
public enum TaskStatus
{
    New,
    Assigned,
    InProgress,
    Completed,
    Cancelled,
    OnHold,
    Blocked
}

/// <summary>
/// Базовый класс для всех задач (общие поля)
/// Содержит информацию из basetasks и activeassignedtasks
/// </summary>
public abstract class TaskItemBase
{
    /// <summary>
    /// ID задачи из basetasks.taskid
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// Тип задачи (Inventory, Receipt, etc.)
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// ID филиала, к которому относится задача
    /// </summary>
    public int BranchId { get; set; }

    /// <summary>
    /// Название задачи
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Описание задачи
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Статус выполнения задачи
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Приоритет (от 1 до 10)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Дата создания задачи
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата завершения задачи (если завершена)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// ID сотрудника, которому назначена задача
    /// </summary>
    public int AssignedToEmployeeId { get; set; }

    /// <summary>
    /// Дата назначения задачи
    /// </summary>
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// Строка назначения инвентаризации (товар для учёта)
/// Соответствует inventoryassignmentlines
/// </summary>
public sealed class InventoryLineItem
{
    public int LineId { get; set; }
    public int ItemPositionId { get; set; }
    public int ExpectedQuantity { get; set; }
    public int? ActualQuantity { get; set; }

    /// <summary>
    /// Структурированное расположение позиции.
    /// ZoneCode/FirstLevelStorageType/FlsNumber считаются устаревшими и не используются.
    /// </summary>
    public PositionCodeInfo? PositionCode { get; set; }
}

/// <summary>
/// Задача инвентаризации (специализированный класс)
/// Содержит данные из inventoryassignments и inventoryassignmentlines
/// </summary>
public sealed class InventoryTaskItem : TaskItemBase
{
    /// <summary>
    /// ID назначения инвентаризации (inventoryassignments.id)
    /// </summary>
    public int AssignmentId { get; set; }

    /// <summary>
    /// Список строк (товаров) для инвентаризации
    /// </summary>
    public List<InventoryLineItem> Lines { get; set; } = new();
}
