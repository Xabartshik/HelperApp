using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Models.Tasks;
using System.Collections.ObjectModel;

namespace HelperApp.ViewModels;

/// <summary>
/// ViewModel для страницы деталей инвентаризации
/// Управляет отображением информации о задаче инвентаризации и списка позиций
/// </summary>
public partial class InventoryDetailsViewModel : ObservableObject
{
    private readonly ILogger<InventoryDetailsViewModel> _logger;
    private int _assignmentId;

    [ObservableProperty]
    private string zoneCodeShortDescription = string.Empty;

    [ObservableProperty]
    private string zoneCodeFullDescription = string.Empty;

    [ObservableProperty]
    private DateTime initiatedAt;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private int totalItems;

    [ObservableProperty]
    private int scannedItemsCount;

    [ObservableProperty]
    private int varianceCount;

    [ObservableProperty]
    private bool hasVariances;

    [ObservableProperty]
    private ObservableCollection<InventoryItemVm> inventoryItems = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public InventoryDetailsViewModel(ILogger<InventoryDetailsViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Инициализация ViewModel с данными из навигационного параметра
    /// </summary>
    public async Task InitializeAsync()
    {
        // Получаем параметр AssignmentId из query string
        if (!int.TryParse(Shell.Current?.CurrentState?.Location?.OriginalString?.Split('=').LastOrDefault(), out _assignmentId))
        {
            ErrorMessage = "Ошибка загрузки данных задачи";
            _logger.LogError("Не удалось получить AssignmentId из параметров навигации");
            return;
        }

        await LoadInventoryDetailsAsync();
    }

    /// <summary>
    /// Загрузить детали инвентаризации
    /// Примечание: в реальном приложении это должны быть данные из API
    /// </summary>
    private async Task LoadInventoryDetailsAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            // Имитация загрузки данных
            // В реальном приложении здесь будет вызов API сервиса
            await LoadMockDataAsync();

            _logger.LogInformation("Детали инвентаризации загружены для AssignmentId={AssignmentId}", _assignmentId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
            _logger.LogError(ex, "Ошибка при загрузке деталей инвентаризации");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Загрузить тестовые данные (для демонстрации)
    /// </summary>
    private async Task LoadMockDataAsync()
    {
        // Имитируем задержку сети
        await Task.Delay(500);

        // Тестовые данные
        InitiatedAt = DateTime.Now.AddHours(-2);
        Status = "В процессе";
        Description = "Инвентаризация склада зоны A";
        ZoneCodeShortDescription = "ZA-RACK-A1";
        ZoneCodeFullDescription = "1-ZA-RACK-A1-S1";

        // Создаем тестовые позиции
        var items = new List<InventoryItemVm>
        {
            new()
            {
                ItemName = "Болт 8x20",
                PositionCode = "ZA-RACK-A1-S1-C1",
                ExpectedQuantity = 100,
                ActualQuantity = 98,
                ItemId = 1
            },
            new()
            {
                ItemName = "Гайка М8",
                PositionCode = "ZA-RACK-A1-S1-C2",
                ExpectedQuantity = 50,
                ActualQuantity = 50,
                ItemId = 2
            },
            new()
            {
                ItemName = "Подшипник 6205",
                PositionCode = "ZA-RACK-A1-S1-C3",
                ExpectedQuantity = 25,
                ActualQuantity = null,
                ItemId = 3
            },
            new()
            {
                ItemName = "Втулка пластиковая",
                PositionCode = "ZA-RACK-A1-S2-C1",
                ExpectedQuantity = 200,
                ActualQuantity = 205,
                ItemId = 4
            },
            new()
            {
                ItemName = "Прокладка резиновая",
                PositionCode = "ZA-RACK-A1-S2-C2",
                ExpectedQuantity = 150,
                ActualQuantity = 150,
                ItemId = 5
            }
        };

        InventoryItems.Clear();
        foreach (var item in items)
        {
            InventoryItems.Add(item);
        }

        // Вычисляем статистику
        TotalItems = InventoryItems.Count;
        ScannedItemsCount = InventoryItems.Count(i => i.ActualQuantity.HasValue);
        VarianceCount = InventoryItems.Count(i => i.ActualQuantity.HasValue && i.ActualQuantity != i.ExpectedQuantity);
        HasVariances = VarianceCount > 0;
    }

    /// <summary>
    /// Команда для возврата на предыдущую страницу
    /// </summary>
    [RelayCommand]
    public async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Очистка ресурсов при закрытии страницы
    /// </summary>
    public void Cleanup()
    {
        // Здесь можно добавить логику очистки, если необходимо
        _logger.LogDebug("InventoryDetailsViewModel очищена");
    }
}

/// <summary>
/// ViewModel для элемента инвентаризации (позиции)
/// Используется в CollectionView на странице InventoryDetailsPage
/// </summary>
public class InventoryItemVm
{
    /// <summary>
    /// ID элемента
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// Название товара
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Код позиции (адрес в хранилище)
    /// </summary>
    public string PositionCode { get; set; } = string.Empty;

    /// <summary>
    /// Ожидаемое количество
    /// </summary>
    public int ExpectedQuantity { get; set; }

    /// <summary>
    /// Фактическое количество (null если не отсканировано)
    /// </summary>
    public int? ActualQuantity { get; set; }

    /// <summary>
    /// Отображаемое фактическое количество
    /// </summary>
    public string ActualQuantityDisplay => ActualQuantity?.ToString() ?? "—";

    /// <summary>
    /// Цвет отображения фактического количества
    /// Красный для расхождений, фиолетовый для совпадений, серый для не отсканировано
    /// </summary>
    public Color ActualQuantityColor
    {
        get
        {
            if (!ActualQuantity.HasValue)
                return Color.FromArgb("#888888"); // Серый для не отсканировано

            if (ActualQuantity == ExpectedQuantity)
                return Color.FromArgb("#7c3aed"); // Фиолетовый для совпадения

            return Color.FromArgb("#ff6b6b"); // Красный для расхождения
        }
    }

    /// <summary>
    /// Завершена ли позиция (отсканирована)
    /// </summary>
    public bool IsCompleted => ActualQuantity.HasValue;

    /// <summary>
    /// Текст статуса позиции
    /// </summary>
    public string StatusText
    {
        get
        {
            if (!ActualQuantity.HasValue)
                return "Не отсканирована";

            if (ActualQuantity == ExpectedQuantity)
                return "✓ Совпадение";

            var difference = ActualQuantity.Value - ExpectedQuantity;
            return difference > 0 ? $"⚠ +{difference}" : $"⚠ {difference}";
        }
    }
}
