namespace TemplateNavegacaoMaui.Pages;

public partial class DetailsPage : ContentPage
{
    public DetailsPage()
    {
        InitializeComponent();
    }

    private async void OnOpenNextClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SecondaryDetailsPage));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
