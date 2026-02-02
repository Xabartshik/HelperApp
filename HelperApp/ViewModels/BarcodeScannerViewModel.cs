using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HelperApp.Messages;
using ZXing.Net.Maui;

namespace HelperApp.ViewModels;

[QueryProperty(nameof(PositionCode), "positionCode")]
public partial class BarcodeScannerViewModel : ObservableObject
{
    private readonly ILogger<BarcodeScannerViewModel> _logger;

    // антидребезг (ZXing может несколько раз подряд прислать один и тот же код)
    private string? _lastBarcode;
    private DateTime _lastBarcodeUtc = DateTime.MinValue;

    [ObservableProperty] private string positionCode = string.Empty;
    [ObservableProperty] private string scannedBarcode = string.Empty;

    [ObservableProperty] private string statusMessage = "Наведите камеру на штрих-код";
    [ObservableProperty] private Color statusColor = Colors.White;

    [ObservableProperty] private bool isScanning = true;
    [ObservableProperty] private bool isProcessing = false;

    // новое: ждём тапа для продолжения
    [ObservableProperty] private bool isAwaitingTap;
    [ObservableProperty] private string tapToContinueMessage = "Нажмите на экран, чтобы продолжить";

    public BarcodeScannerViewModel(ILogger<BarcodeScannerViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Если у вас уже реализован ответ обратно (успех/ошибка) — покажем его на экране сканера
        WeakReferenceMessenger.Default.Register<BarcodeProcessedMessage>(this, (_, message) =>
        {
            if (!string.Equals(message.Value.PositionCode, PositionCode, StringComparison.Ordinal))
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = message.Value.Message;
                StatusColor = message.Value.IsSuccess
                    ? Color.FromArgb("#22c55e")
                    : Color.FromArgb("#ff6b6b");
            });
        });
    }

    public void OnBarcodeDetected(BarcodeDetectionEventArgs e)
    {
        // если уже ждём тапа — игнорируем любые новые детекты
        if (IsAwaitingTap)
            return;

        if (IsProcessing)
            return;

        var barcode = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(barcode))
            return;

        // антидребезг
        var nowUtc = DateTime.UtcNow;
        if (string.Equals(_lastBarcode, barcode, StringComparison.Ordinal)
            && (nowUtc - _lastBarcodeUtc) < TimeSpan.FromMilliseconds(900))
        {
            return;
        }

        _lastBarcode = barcode;
        _lastBarcodeUtc = nowUtc;

        IsProcessing = true;
        IsScanning = false;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                ScannedBarcode = barcode;

                // базовое подтверждение “считали”
                StatusMessage = $"Сканировано: {barcode}";
                StatusColor = Color.FromArgb("#7c3aed");

                _logger.LogInformation("Штрих-код отсканирован: {Barcode}, позиция={PositionCode}", barcode, PositionCode);

                // ВАЖНО: сканер НЕ закрываем, а отправляем сообщение в детали инвентаризации
                WeakReferenceMessenger.Default.Send(
                    new BarcodeScannedMessage(new BarcodeScannedPayload(PositionCode, barcode)));

                // переходим в режим ожидания тапа
                IsAwaitingTap = true;
                TapToContinueMessage = "Нажмите на экран, чтобы продолжить";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки штрих-кода");
                StatusMessage = "Ошибка обработки штрих-кода";
                StatusColor = Color.FromArgb("#ff6b6b");

                // после ошибки — тоже требуем тап, чтобы не зациклиться
                IsAwaitingTap = true;
                TapToContinueMessage = "Ошибка. Нажмите на экран, чтобы попробовать снова";
            }
            finally
            {
                IsProcessing = false;
            }
        });
    }

    [RelayCommand]
    private void ContinueScanning()
    {
        // снимаем оверлей и снова включаем детект
        IsAwaitingTap = false;
        StatusMessage = "Наведите камеру на штрих-код";
        StatusColor = Colors.White;
        IsScanning = true;
    }

    [RelayCommand]
    private async Task Cancel()
    {
        IsScanning = false;
        IsProcessing = false;
        IsAwaitingTap = false;

        await Shell.Current.GoToAsync("..");
    }

    public void Cleanup()
    {
        IsScanning = false;
        IsProcessing = false;
        IsAwaitingTap = false;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        _logger.LogDebug("BarcodeScannerViewModel очищена");
    }
}
