using HelperApp.Services;

namespace HelperApp.Models.Tasks;

/// <summary>
/// ViewModel для карточки задачи (для отображения в UI)
/// Содержит преобразованные данные из TaskItemBase для красивого отображения
/// </summary>
public sealed class TaskCardVm
{
    /// <summary>
    /// ID задачи (для идентификации при клике)
    /// </summary>
    public int Id { get; set; }

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
        var completedCount = task.Lines.Count(l => l.ActualQuantity.HasValue);
        var totalCount = task.Lines.Count;
        var varianceCount = task.Lines.Count(l =>
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
            Id = task.TaskId,
            Title = task.Title,
            Subtitle = task.Description,
            StatusText = task.Status.ToString(),
            PrimaryMetric = primaryMetric,
            CreatedAt = task.CreatedAt,
            Badges = badges.ToList(),
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
            Id = task.TaskId,
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