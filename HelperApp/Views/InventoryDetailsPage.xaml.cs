using HelperApp.ViewModels;

namespace HelperApp.Views;

[QueryProperty(nameof(ScannedCode), "scannedCode")]
public partial class InventoryDetailsPage : ContentPage
{
    private readonly InventoryDetailsViewModel _viewModel;
    private string? _scannedCode;

    public string? ScannedCode
    {
        get => _scannedCode;
        set
        {
            _scannedCode = value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                // Передаем отсканированный код в активную группу
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ProcessScannedCodeAsync(value);
                });
            }
        }
    }

    public InventoryDetailsPage(InventoryDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Cleanup();
    }

    private async Task ProcessScannedCodeAsync(string scannedCode)
    {
        // Находим раскрытую (активную) группу
        var activeGroup = _viewModel.GroupedInventoryItems.FirstOrDefault(g => g.IsExpanded);

        if (activeGroup != null)
        {
            await activeGroup.ProcessScannedCodeAsync(scannedCode);
        }
        else
        {
            await DisplayAlert("Ошибка", "Не выбрана активная группа для сканирования", "OK");
        }
    }
}
