using HelperApp.ViewModels;

namespace HelperApp.Views;

public partial class InventoryDetailsPage : ContentPage
{
    private readonly InventoryDetailsViewModel _viewModel;

    public InventoryDetailsPage(InventoryDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is InventoryDetailsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is InventoryDetailsViewModel viewModel)
        {
            viewModel.Cleanup();
        }
    }
}
