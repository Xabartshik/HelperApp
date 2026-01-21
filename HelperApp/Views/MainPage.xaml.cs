using HelperApp.ViewModels;

namespace HelperApp.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
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
        // Важно: НЕ делать logout здесь.
        // При необходимости можно останавливать только фоновые таймеры/синхронизацию.
        _viewModel.StopPeriodicSync();
    }
}
