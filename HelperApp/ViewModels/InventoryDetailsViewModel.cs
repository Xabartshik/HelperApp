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

                GroupedInventoryItems.Add(new InventoryGroupVm
                {
                    PositionCode = group.Key,
                    Items = new ObservableCollection<InventoryItemVm>(items),
                    IsExpanded = false
                });
            }

            // Stats
            var allItems = GroupedInventoryItems.SelectMany(g => g.Items).ToList();
            TotalItems = allItems.Count;
            ScannedItemsCount = allItems.Count(i => i.ActualQuantity.HasValue);
            VarianceCount = allItems.Count(i => i.ActualQuantity.HasValue && i.ActualQuantity != i.ExpectedQuantity);
            HasVariances = VarianceCount > 0;

            _logger.LogInformation("Детали инвентаризации загружены (workerId={WorkerId}, assignmentId={AssignmentId})", WorkerId, AssignmentId);
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

    [RelayCommand]
    public Task GoBack() => Shell.Current.GoToAsync("..");

    public void Cleanup()
    {
        _logger.LogDebug("InventoryDetailsViewModel очищена");
    }
}

/// <summary>
/// Группа позиций инвентаризации по коду позиции товара
/// </summary>
public partial class InventoryGroupVm : ObservableObject
{
    [ObservableProperty] private string positionCode = string.Empty;
    [ObservableProperty] private ObservableCollection<InventoryItemVm> items = new();
    [ObservableProperty] private bool isExpanded;

    public int ItemCount => Items?.Count ?? 0;

    public int ExpectedTotalQuantity => Items?.Sum(i => i.ExpectedQuantity) ?? 0;

    public int ActualTotalQuantity => Items?.Sum(i => i.ActualQuantity ?? 0) ?? 0;

    public int ScannedCount => Items?.Count(i => i.ActualQuantity.HasValue) ?? 0;

    public string GroupHeader => $"Код: {PositionCode} ({ItemCount} шт.)";

    public string ScanButtonText => IsExpanded ? "Сканировать" : "";

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ScanButtonText));
    }

    [RelayCommand]
    private async Task ScanItems()
    {
        // Здесь будет логика сканирования
        await Application.Current.MainPage.DisplayAlert("Сканирование", $"Сканирование позиций с кодом {PositionCode}", "OK");
    }
}

public class InventoryItemVm
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int? ActualQuantity { get; set; }

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
}
