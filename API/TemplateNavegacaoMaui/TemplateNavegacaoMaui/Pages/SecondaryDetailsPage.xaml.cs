namespace TemplateNavegacaoMaui.Pages;

public partial class SecondaryDetailsPage : ContentPage
{
    public SecondaryDetailsPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
