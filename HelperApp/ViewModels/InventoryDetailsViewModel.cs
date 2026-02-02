using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HelperApp.Messages;
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

    // Чтобы не перезагружать и не затирать ActualQuantity при каждом OnAppearing (например, при возврате со сканера)
    private int _loadedWorkerId;
    private int _loadedAssignmentId;
    private bool _isLoaded;

    private bool _messengerRegistered;

    public InventoryDetailsViewModel(
        ILogger<InventoryDetailsViewModel> logger,
        IApiClient apiClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

        RegisterMessenger();
    }

    private void RegisterMessenger()
    {
        if (_messengerRegistered)
            return;

        WeakReferenceMessenger.Default.Register<BarcodeScannedMessage>(this, (_, message) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await HandleBarcodeScannedAsync(message.Value);
            });
        });

        _messengerRegistered = true;
    }

    private async Task HandleBarcodeScannedAsync(BarcodeScannedPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.PositionCode) || string.IsNullOrWhiteSpace(payload.Barcode))
            return;

        // Если данные ещё не загружены — просто игнорируем (сканер всё равно может продолжать, но применять будет некуда)
        if (GroupedInventoryItems.Count == 0)
            return;

        var group = GroupedInventoryItems.FirstOrDefault(g =>
            string.Equals(g.PositionCode, payload.PositionCode, StringComparison.Ordinal));

        if (group is null)
        {
            WeakReferenceMessenger.Default.Send(new BarcodeProcessedMessage(
                new BarcodeProcessedPayload(payload.PositionCode, payload.Barcode, false, "Позиция не найдена в текущей задаче")));
            return;
        }

        var result = await group.ProcessScannedCodeAsync(payload.Barcode, _apiClient, () =>
        {
            UpdateStatistics();
            OnPropertyChanged(nameof(GroupedInventoryItems));
        });

        // Статистика обновится через ItemsUpdated, но если код не применился — тоже отдадим фидбек
        if (result.IsApplied)
            UpdateStatistics();

        WeakReferenceMessenger.Default.Send(new BarcodeProcessedMessage(
            new BarcodeProcessedPayload(payload.PositionCode, payload.Barcode, result.IsApplied, result.Message)));
    }

    public async Task InitializeAsync()
    {
        if (WorkerId <= 0 || AssignmentId <= 0)
        {
            ErrorMessage = "Ошибка загрузки: не переданы параметры workerId/assignmentId";
            _logger.LogError("Не переданы параметры workerId/assignmentId (workerId={WorkerId}, assignmentId={AssignmentId})", WorkerId, AssignmentId);
            return;
        }

        if (_isLoaded
            && _loadedWorkerId == WorkerId
            && _loadedAssignmentId == AssignmentId
            && GroupedInventoryItems.Count > 0)
        {
            return;
        }

        await LoadInventoryDetailsAsync();

        _loadedWorkerId = WorkerId;
        _loadedAssignmentId = AssignmentId;
        _isLoaded = true;
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

            Status = "В процессе";
            Description = $"Инвентаризация зоны {dto.ZoneCode}";

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
    public async Task GoBack()
    {
        Cleanup();
        await Shell.Current.GoToAsync("..");
    }

    public void Cleanup()
    {
        foreach (var group in GroupedInventoryItems)
            group.ItemsUpdated -= OnGroupItemsUpdated;

        if (_messengerRegistered)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            _messengerRegistered = false;
        }

        _logger.LogDebug("InventoryDetailsViewModel очищена");
    }
}

public sealed record ScanApplyResult(bool IsApplied, string Message);

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
    public int ExpectedTotalQuantity => Items?.Sum(i => i.ExpectedQuantity ?? 0) ?? 0;
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
    /// Обрабатывает отсканированный код (без UI-диалогов, чтобы не мешать непрерывному сканированию)
    /// </summary>
    public async Task<ScanApplyResult> ProcessScannedCodeAsync(string scannedCode, IApiClient apiClient, Action onItemsChanged)
    {
        if (string.IsNullOrWhiteSpace(scannedCode))
            return new ScanApplyResult(false, "Пустой код");

        try
        {
            _logger.LogInformation("Обработка кода: {Code} для позиции {PositionCode}", scannedCode, PositionCode);

            // Текущее допущение проекта: scannedCode == ItemId (строкой)
            if (!int.TryParse(scannedCode, out int itemId))
                return new ScanApplyResult(false, $"Неверный формат: {scannedCode}");

            var item = Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item is null)
            {
                // Товар не найден среди ожидаемых - запрашиваем информацию у сервера
                try
                {
                    var itemInfo = await apiClient.GetItemInfoAsync(itemId);
                    if (itemInfo == null)
                        return new ScanApplyResult(false, $"Товар с ID {itemId} не найден в базе");

                    // Показываем диалог с выбором
                    bool shouldAddItem = await Application.Current.MainPage.DisplayAlert(
                        "Товар не найден",
                        $"Товар \"{itemInfo.Name}\" не найден в ожидаемых для данной позиции.\n\nУчесть товар?",
                        "Учесть товар",
                        "Отмена");

                    if (shouldAddItem)
                    {
                        // Создаём новый элемент без ожидаемого количества
                        var newItem = new InventoryItemVm
                        {
                            ItemId = itemInfo.ItemId,
                            ItemName = itemInfo.Name,
                            PositionCode = PositionCode,
                            ExpectedQuantity = null,
                            ActualQuantity = 1
                        };

                        Items.Add(newItem);

                        _logger.LogInformation("Добавлен неожиданный товар {ItemName} (ID: {ItemId}) в позицию {PositionCode}",
                            itemInfo.Name, itemInfo.ItemId, PositionCode);

                        OnPropertyChanged(nameof(ItemCount));
                        OnPropertyChanged(nameof(ScannedCount));
                        OnPropertyChanged(nameof(ActualTotalQuantity));
                        ItemsUpdated?.Invoke(this, EventArgs.Empty);
                        onItemsChanged?.Invoke();

                        return new ScanApplyResult(true, $"✓ {itemInfo.Name}: добавлен (1)");
                    }
                    else
                    {
                        return new ScanApplyResult(false, "Отменено пользователем");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при получении информации о товаре {ItemId}", itemId);
                    return new ScanApplyResult(false, $"Ошибка при получении информации о товаре: {ex.Message}");
                }
            }

            item.ActualQuantity = (item.ActualQuantity ?? 0) + 1;

            _logger.LogInformation("Товар {ItemName} (ID: {ItemId}) учтён. Факт={ActualQuantity}", item.ItemName, item.ItemId, item.ActualQuantity);

            OnPropertyChanged(nameof(ScannedCount));
            OnPropertyChanged(nameof(ActualTotalQuantity));
            ItemsUpdated?.Invoke(this, EventArgs.Empty);

            return new ScanApplyResult(true, $"✓ {item.ItemName}: {item.ActualQuantity}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке кода");
            return new ScanApplyResult(false, "Ошибка обработки кода");
        }
    }
}

public partial class InventoryItemVm : ObservableObject
{
    [ObservableProperty] private int itemId;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string positionCode = string.Empty;
    [ObservableProperty] private int? expectedQuantity;
    [ObservableProperty] private int? actualQuantity;

    public string ExpectedQuantityDisplay => ExpectedQuantity?.ToString() ?? "—";
    public string ActualQuantityDisplay => ActualQuantity?.ToString() ?? "—";

    public Color ActualQuantityColor
    {
        get
        {
            if (!ActualQuantity.HasValue) return Color.FromArgb("#888888");
            if (!ExpectedQuantity.HasValue) return Color.FromArgb("#ff6b6b"); // неожиданный товар - красный
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
            if (!ExpectedQuantity.HasValue) return "⚠ Неожиданный товар";
            if (ActualQuantity == ExpectedQuantity) return "✓ Совпадение";
            var diff = ActualQuantity.Value - ExpectedQuantity.Value;
            return diff > 0 ? $"⚠ +{diff}" : $"⚠ {diff}";
        }
    }

    partial void OnExpectedQuantityChanged(int? value)
    {
        OnPropertyChanged(nameof(ExpectedQuantityDisplay));
        OnPropertyChanged(nameof(ActualQuantityColor));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnActualQuantityChanged(int? value)
    {
        OnPropertyChanged(nameof(ActualQuantityDisplay));
        OnPropertyChanged(nameof(ActualQuantityColor));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(StatusText));
    }
}
