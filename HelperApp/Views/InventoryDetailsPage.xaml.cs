using HelperApp.ViewModels;

namespace HelperApp.Views;

public partial class InventoryDetailsPage : ContentPage
{
    public InventoryDetailsPage()
    {
        InitializeComponent();
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
