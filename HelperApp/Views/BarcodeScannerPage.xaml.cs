using HelperApp.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace HelperApp.Views;

public partial class BarcodeScannerPage : ContentPage
{
    private readonly BarcodeScannerViewModel _viewModel;

    public BarcodeScannerPage(BarcodeScannerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;


        cameraBarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.Code128 | BarcodeFormat.Ean13 | BarcodeFormat.Ean8,
            AutoRotate = true,
            Multiple = false,
            TryHarder = false,
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.IsScanning = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.IsScanning = false;
        _viewModel.Cleanup();
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        _viewModel.OnBarcodeDetected(e);
    }
}
