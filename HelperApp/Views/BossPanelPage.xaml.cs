using HelperApp.ViewModels;

namespace HelperApp.Views;

public partial class BossPanelPage : ContentPage
{
    private readonly BossPanelViewModel _viewModel;

    public BossPanelPage(BossPanelViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }

    private void OnTab1Clicked(object sender, EventArgs e)
    {
        UpdateButtonStyles((Button)sender);
        Tab1View.IsVisible = true;
        Tab2View.IsVisible = false;
        Tab3View.IsVisible = false;
    }

    private void OnTab2Clicked(object sender, EventArgs e)
    {
        UpdateButtonStyles((Button)sender);
        Tab1View.IsVisible = false;
        Tab2View.IsVisible = true;
        Tab3View.IsVisible = false;
    }

    private void OnTab3Clicked(object sender, EventArgs e)
    {
        UpdateButtonStyles((Button)sender);
        Tab1View.IsVisible = false;
        Tab2View.IsVisible = false;
        Tab3View.IsVisible = true;
    }

    private void UpdateButtonStyles(Button selectedButton)
    {
        var parent = (HorizontalStackLayout)selectedButton.Parent;
        foreach (var child in parent.Children)
        {
            if (child is Button btn)
            {
                if (btn == selectedButton)
                {
                    btn.TextColor = Colors.White;
                    btn.FontAttributes = FontAttributes.Bold;
                }
                else
                {
                    btn.TextColor = Colors.Gray;
                    btn.FontAttributes = FontAttributes.None;
                }
            }
        }
    }
}
