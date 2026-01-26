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

    [ObservableProperty] private ObservableCollection<InventoryItemVm> inventoryItems = new();

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

            // Items
            InventoryItems.Clear();

            foreach (var item in dto.Items)
            {
                InventoryItems.Add(new InventoryItemVm
                {
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    PositionCode = item.PositionCode,
                    ExpectedQuantity = item.ExpectedQuantity,

                    // На вашем GET details фактического количества нет, поэтому оставляем null
                    ActualQuantity = null
                });
            }

            // Stats (как раньше)
            TotalItems = InventoryItems.Count;
            ScannedItemsCount = InventoryItems.Count(i => i.ActualQuantity.HasValue);
            VarianceCount = InventoryItems.Count(i => i.ActualQuantity.HasValue && i.ActualQuantity != i.ExpectedQuantity);
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
