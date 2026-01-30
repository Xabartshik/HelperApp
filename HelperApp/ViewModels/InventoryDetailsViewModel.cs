using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Models;
using HelperApp.Services;
using System.Collections.ObjectModel;

namespace HelperApp.ViewModels;

[QueryProperty(nameof(AssignmentId), "assignmentId")]
[QueryProperty(nameof(WorkerId), "workerId")]
public partial class InventoryDetailsViewModel : ObservableObject
{
    private readonly ILogger<InventoryDetailsViewModel> _logger;
    private readonly IApiClient _apiClient;

    // приходит из навигации
    [ObservableProperty] private int assignmentId;
    [ObservableProperty] private int workerId;

    [ObservableProperty] private string zoneCodeShortDescription = string.Empty;
    [ObservableProperty] private string zoneCodeFullDescription = string.Empty;
    [ObservableProperty] private DateTime initiatedAt;
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private string description = string.Empty;

    [ObservableProperty] private int totalItems;
    [ObservableProperty] private int scannedItemsCount;
    [ObservableProperty] private int varianceCount;
    [ObservableProperty] private bool hasVariances;

    // Сгруппированные позиции по PositionCode
    [ObservableProperty] private ObservableCollection<InventoryGroupVm> groupedInventoryItems = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;

    public InventoryDetailsViewModel(
        ILogger<InventoryDetailsViewModel> logger,
        IApiClient apiClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task InitializeAsync()
    {
        if (WorkerId <= 0 || AssignmentId <= 0)
        {
            ErrorMessage = "Ошибка загрузки: не переданы параметры workerId/assignmentId";
            _logger.LogError("Не переданы параметры workerId/assignmentId (workerId={WorkerId}, assignmentId={AssignmentId})", WorkerId, AssignmentId);
            return;
        }

        await LoadInventoryDetailsAsync();
    }

    private async Task LoadInventoryDetailsAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var dto = await _apiClient.GetInventoryTaskDetailsAsync(WorkerId, AssignmentId);
            if (dto is null)
            {
                ErrorMessage = "Сервер вернул пустой ответ";
                return;
            }

            // Header
            InitiatedAt = dto.InitiatedAt;
            ZoneCodeShortDescription = dto.ZoneCode;
            ZoneCodeFullDescription = dto.ZoneCode;

            // Эти поля в DTO сейчас не приходят — задаём осмысленные значения
            Status = "В процессе";
            Description = $"Инвентаризация зоны {dto.ZoneCode}";

            // Группировка позиций по PositionCode
            GroupedInventoryItems.Clear();

            var grouped = dto.Items
                .GroupBy(item => item.PositionCode)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var items = group.Select(item => new InventoryItemVm
                {
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    PositionCode = item.PositionCode,
                    ExpectedQuantity = item.ExpectedQuantity,
                    ActualQuantity = null
                }).ToList();

                var groupVm = new InventoryGroupVm(_logger)
                {
                    PositionCode = group.Key,
                    Items = new ObservableCollection<InventoryItemVm>(items),
                    IsExpanded = false
                };

                // Подписываемся на событие обновления статистики
                groupVm.ItemsUpdated += OnGroupItemsUpdated;

                GroupedInventoryItems.Add(groupVm);
            }

            UpdateStatistics();

            _logger.LogInformation("Детали инвентаризации загружены (workerId={WorkerId}, assignmentId={AssignmentId}). Групп: {GroupCount}", WorkerId, AssignmentId, GroupedInventoryItems.Count);
        }
        catch (ApiClient.NoNetworkException)
        {
            ErrorMessage = "Нет подключения к сети";
        }
        catch (ApiClient.UnauthorizedException)
        {
            ErrorMessage = "Сессия истекла. Войдите заново.";
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

    private void OnGroupItemsUpdated(object? sender, EventArgs e)
    {
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var allItems = GroupedInventoryItems.SelectMany(g => g.Items).ToList();
        TotalItems = allItems.Count;
        ScannedItemsCount = allItems.Count(i => i.ActualQuantity.HasValue);
        VarianceCount = allItems.Count(i => i.ActualQuantity.HasValue && i.ActualQuantity != i.ExpectedQuantity);
        HasVariances = VarianceCount > 0;
    }

    [RelayCommand]
    public Task GoBack() => Shell.Current.GoToAsync("..");

    public void Cleanup()
    {
        // Отписываемся от событий
        foreach (var group in GroupedInventoryItems)
        {
            group.ItemsUpdated -= OnGroupItemsUpdated;
        }

        _logger.LogDebug("InventoryDetailsViewModel очищена");
    }
}

/// <summary>
/// Группа позиций инвентаризации по коду позиции товара
/// </summary>
public partial class InventoryGroupVm : ObservableObject
{
    private readonly ILogger _logger;

    [ObservableProperty] private string positionCode = string.Empty;
    [ObservableProperty] private ObservableCollection<InventoryItemVm> items = new();
    [ObservableProperty] private bool isExpanded;

    public event EventHandler? ItemsUpdated;

    public int ItemCount => Items?.Count ?? 0;

    public int ExpectedTotalQuantity => Items?.Sum(i => i.ExpectedQuantity) ?? 0;

    public int ActualTotalQuantity => Items?.Sum(i => i.ActualQuantity ?? 0) ?? 0;

    public int ScannedCount => Items?.Count(i => i.ActualQuantity.HasValue) ?? 0;

    public string GroupHeader => $"Код: {PositionCode} ({ItemCount} шт.)";

    public string ScanButtonText => IsExpanded ? "Сканировать" : "";

    public InventoryGroupVm(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ScanButtonText));
    }

    [RelayCommand]
    private async Task ScanItems()
    {
        try
        {
            // Переходим на страницу сканирования, передавая код позиции
            await Shell.Current.GoToAsync("scanner", new Dictionary<string, object>
            {
                ["positionCode"] = PositionCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при открытии сканера");
            await Application.Current.MainPage.DisplayAlert("Ошибка", $"Не удалось открыть сканер: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Обрабатывает отсканированный код
    /// </summary>
    public async Task ProcessScannedCodeAsync(string scannedCode)
    {
        if (string.IsNullOrWhiteSpace(scannedCode))
            return;

        try
        {
            _logger.LogInformation("Обработка отсканированного кода: {Code} для позиции {PositionCode}", scannedCode, PositionCode);

            // Для упрощения считаем, что scannedCode == ItemId (в виде строки)
            if (!int.TryParse(scannedCode, out int itemId))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ошибка",
                    $"Неверный формат штрих-кода: {scannedCode}",
                    "OK");
                return;
            }

            // Ищем товар с таким ItemId среди ожидаемых в этой группе
            var item = Items.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                // Товар найден среди ожидаемых - увеличиваем фактическое количество
                item.ActualQuantity = (item.ActualQuantity ?? 0) + 1;

                _logger.LogInformation(
                    "Товар {ItemName} (ID: {ItemId}) отсканирован. Фактическое количество: {ActualQuantity}",
                    item.ItemName, item.ItemId, item.ActualQuantity);

                // Обновляем UI
                OnPropertyChanged(nameof(ScannedCount));
                OnPropertyChanged(nameof(ActualTotalQuantity));

                // Уведомляем родительскую ViewModel об изменениях
                ItemsUpdated?.Invoke(this, EventArgs.Empty);

                await Application.Current.MainPage.DisplayAlert(
                    "Успешно",
                    $"Отсканирован: {item.ItemName}\nФактическое количество: {item.ActualQuantity}",
                    "OK");
            }
            else
            {
                // Товар НЕ найден среди ожидаемых для этого кода позиции
                _logger.LogWarning(
                    "Отсканирован неожиданный товар с ID {ItemId} для позиции {PositionCode}",
                    itemId, PositionCode);

                // TODO: Обработка неожиданного товара
                var result = await Application.Current.MainPage.DisplayAlert(
                    "Неожиданный товар",
                    $"Товар с кодом {scannedCode} не найден среди ожидаемых позиций для кода {PositionCode}.\n\n" +
                    "Это может быть:\n" +
                    "• Лишний товар на этой позиции\n" +
                    "• Товар с другой позиции\n" +
                    "• Ошибка сканирования\n\n" +
                    "Добавить как незапланированный товар?",
                    "Да",
                    "Нет");

                if (result)
                {
                    // TODO: Реализовать добавление незапланированного товара
                    await Application.Current.MainPage.DisplayAlert(
                        "TODO",
                        "Функция добавления незапланированных товаров будет реализована позже",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке отсканированного кода");
            await Application.Current.MainPage.DisplayAlert(
                "Ошибка",
                $"Не удалось обработать отсканированный код: {ex.Message}",
                "OK");
        }
    }
}

public partial class InventoryItemVm : ObservableObject
{
    [ObservableProperty] private int itemId;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string positionCode = string.Empty;
    [ObservableProperty] private int expectedQuantity;
    [ObservableProperty] private int? actualQuantity;

    public string ActualQuantityDisplay => ActualQuantity?.ToString() ?? "—";

    public Color ActualQuantityColor
    {
        get
        {
            if (!ActualQuantity.HasValue) return Color.FromArgb("#888888");
            if (ActualQuantity == ExpectedQuantity) return Color.FromArgb("#7c3aed");
            return Color.FromArgb("#ff6b6b");
        }
    }

    public bool IsCompleted => ActualQuantity.HasValue;

    public string StatusText
    {
        get
        {
            if (!ActualQuantity.HasValue) return "Не отсканирована";
            if (ActualQuantity == ExpectedQuantity) return "✓ Совпадение";
            var diff = ActualQuantity.Value - ExpectedQuantity;
            return diff > 0 ? $"⚠ +{diff}" : $"⚠ {diff}";
        }
    }

    // При изменении ActualQuantity обновляем зависимые свойства
    partial void OnActualQuantityChanged(int? value)
    {
        OnPropertyChanged(nameof(ActualQuantityDisplay));
        OnPropertyChanged(nameof(ActualQuantityColor));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(StatusText));
    }
}
