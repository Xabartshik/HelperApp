using HelperApp.Services;

namespace HelperApp.Models.Tasks;

/// <summary>
/// ViewModel для карточки задачи (для отображения в UI)
/// Содержит преобразованные данные из TaskItemBase для красивого отображения
/// </summary>
public sealed class TaskCardVm
{
    /// <summary>
    /// Тип задачи (для маршрутизации: "Inventory" или "Task")
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// ID для навигации (для Inventory это AssignmentId, иначе TaskId)
    /// </summary>
    public int NavigationId { get; set; }

    /// <summary>
    /// Название задачи
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Подзаголовок (обычно описание)
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Текст статуса (для отображения)
    /// </summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// Основная метрика для отображения (например, "5/10 позиций")
    /// </summary>
    public string? PrimaryMetric { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Список значков/бейджей для отображения (ключ-значение)
    /// </summary>
    public List<KeyValuePair<string, string>> Badges { get; set; } = new();

    /// <summary>
    /// Ссылка на сырую задачу (для получения полной информации при выполнении)
    /// </summary>
    public TaskItemBase? RawTask { get; set; }
}

/// <summary>
/// Маппер для преобразования TaskItemBase в TaskCardVm
/// </summary>
public static class TaskCardMapper
{
    /// <summary>
    /// Преобразование любой задачи (TaskItemBase) в карточку для отображения (TaskCardVm)
    /// </summary>
    public static TaskCardVm ToCard(TaskItemBase task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        return task switch
        {
            InventoryTaskItem inventory => MapInventoryTaskToCard(inventory),
            _ => MapGenericTaskToCard(task)
        };
    }

    /// <summary>
    /// Маппер для InventoryTaskItem → TaskCardVm
    /// </summary>
    private static TaskCardVm MapInventoryTaskToCard(InventoryTaskItem task)
    {
        var lines = task.Lines ?? new List<InventoryLineItem>();
        var completedCount = lines.Count(l => l.ActualQuantity.HasValue);
        var totalCount = lines.Count;
        var varianceCount = lines.Count(l =>
            l.ActualQuantity.HasValue &&
            l.ActualQuantity != l.ExpectedQuantity);

        var primaryMetric = totalCount > 0
            ? $"{completedCount}/{totalCount} позиций"
            : "Нет позиций";

        var badges = new Dictionary<string, string>
        {
            { "Зона", task.ZoneCode ?? "—" },
            { "Статус назначения", task.AssignmentStatus.ToString() },
            { "Расхождений", varianceCount.ToString() }
        };

        return new TaskCardVm
        {
            Kind = TaskType.Inventory.ToString(),
            NavigationId = task.AssignmentId,
            Title = task.Title,
            Subtitle = task.Description,
            StatusText = task.Status.ToString(),
            PrimaryMetric = primaryMetric,
            CreatedAt = task.CreatedAt,
            Badges = badges.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToList(),
            RawTask = task
        };
    }

    /// <summary>
    /// Маппер для общего случая → TaskCardVm
    /// </summary>
    private static TaskCardVm MapGenericTaskToCard(TaskItemBase task)
    {
        return new TaskCardVm
        {
            Kind = task.Type.ToString(),
            NavigationId = task.TaskId,
            Title = task.Title,
            Subtitle = task.Description,
            StatusText = task.Status.ToString(),
            PrimaryMetric = $"Приоритет: {task.Priority}",
            CreatedAt = task.CreatedAt,
            Badges = new List<KeyValuePair<string, string>>
            {
                new("Тип", task.Type.ToString()),
                new("Статус", task.Status.ToString())
            },
            RawTask = task
        };
    }
}