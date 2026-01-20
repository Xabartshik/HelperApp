using HelperApp.ViewModels;

namespace HelperApp.Views;

/// <summary>
/// Page for displaying and managing inventory tasks
/// </summary>
public partial class InventoryTasksPage : ContentPage
{
    private readonly InventoryTasksViewModel _viewModel;
    private bool _isInitialized;

    public InventoryTasksPage(InventoryTasksViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = viewModel;
    }

    /// <summary>
    /// Called when page appears - starts polling for tasks
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_isInitialized)
        {
            InitializeViewModel();
            _isInitialized = true;
        }

        // Start polling when page becomes visible
        _viewModel.StartPolling();
    }

    /// <summary>
    /// Called when page disappears - stops polling
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Stop polling when page is hidden
        _viewModel.StopPollingCommand?.Execute(null);
    }

    /// <summary>
    /// Initialize ViewModel with server configuration
    /// </summary>
    private void InitializeViewModel()
    {
        try
        {
            // Get configuration from app settings or secure storage
            var serverAddress = SecureStorage.GetAsync("server_address").Result 
                ?? AppSettings.DefaultServerAddress;
            var authToken = SecureStorage.GetAsync("auth_token").Result ?? "";

            // Initialize ViewModel
            _viewModel.Initialize(serverAddress, authToken);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Initialization Error", 
                    $"Failed to initialize: {ex.Message}", "OK");
            });
        }
    }
}

/// <summary>
/// Application-wide settings (create this class or use your existing one)
/// </summary>
public static class AppSettings
{
    public const string DefaultServerAddress = "https://localhost:5001";
}
