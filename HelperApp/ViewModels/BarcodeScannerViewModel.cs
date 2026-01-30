using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZXing.Net.Maui;

namespace HelperApp.ViewModels;

[QueryProperty(nameof(PositionCode), "positionCode")]
public partial class BarcodeScannerViewModel : ObservableObject
{
    private readonly ILogger<BarcodeScannerViewModel> _logger;

    [ObservableProperty] private string positionCode = string.Empty;
    [ObservableProperty] private string scannedBarcode = string.Empty;
    [ObservableProperty] private string statusMessage = "Наведите камеру на штрих-код";
    [ObservableProperty] private Color statusColor = Colors.White;
    [ObservableProperty] private bool isScanning = true;
    [ObservableProperty] private bool isProcessing = false;

    public event EventHandler<string>? BarcodeScanned;

    public BarcodeScannerViewModel(ILogger<BarcodeScannerViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnBarcodeDetected(BarcodeDetectionEventArgs e)
    {
        if (IsProcessing)
            return;

        var barcode = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(barcode))
            return;

        IsProcessing = true;
        IsScanning = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                ScannedBarcode = barcode;
                StatusMessage = $"Отсканирован: {barcode}";
                StatusColor = Color.FromArgb("#7c3aed");

                _logger.LogInformation("Штрих-код отсканирован: {Barcode}", barcode);

                // Небольшая задержка для визуализации
                await Task.Delay(500);

                // Уведомляем подписчиков о сканировании
                BarcodeScanned?.Invoke(this, barcode);

                // Автоматически закрываем окно сканера и возвращаем результат
                await Shell.Current.GoToAsync("..", new Dictionary<string, object>
                {
                    ["scannedCode"] = barcode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки штрих-кода");
                StatusMessage = "Ошибка обработки штрих-кода";
                StatusColor = Color.FromArgb("#ff6b6b");
                IsScanning = true;
                IsProcessing = false;
            }
        });
    }

    [RelayCommand]
    private async Task Cancel()
    {
        IsScanning = false;
        await Shell.Current.GoToAsync("..");
    }

    public void Cleanup()
    {
        IsScanning = false;
        _logger.LogDebug("BarcodeScannerViewModel очищена");
    }
}
