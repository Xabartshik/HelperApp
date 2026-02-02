using HelperApp.Services;
using HelperApp.ViewModels;

namespace HelperApp.Views;

[QueryProperty(nameof(ScannedCode), "scannedCode")]
public partial class InventoryDetailsPage : ContentPage
{
    private readonly InventoryDetailsViewModel _viewModel;
    private readonly IApiClient _apiClient;
    private string? _scannedCode;

    public string? ScannedCode
    {
        get => _scannedCode;
        set
        {
            _scannedCode = value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ProcessScannedCodeAsync(value);
                });
            }
        }
    }

    public InventoryDetailsPage(InventoryDetailsViewModel viewModel, IApiClient apiClient)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async Task ProcessScannedCodeAsync(string scannedCode)
    {
        var activeGroup = _viewModel.GroupedInventoryItems.FirstOrDefault(g => g.IsExpanded);
        if (activeGroup != null)
        {
            var result = await activeGroup.ProcessScannedCodeAsync(
                scannedCode,
                _apiClient,
                () =>
                {
                    // Обновление UI при изменении коллекции
                    OnPropertyChanged(nameof(_viewModel.GroupedInventoryItems));
                });

            if (!result.IsApplied)
                await DisplayAlert("Сканирование", result.Message, "OK");
        }
        else
        {
            await DisplayAlert("Ошибка", "Не выбрана активная группа для сканирования", "OK");
        }
    }
}
