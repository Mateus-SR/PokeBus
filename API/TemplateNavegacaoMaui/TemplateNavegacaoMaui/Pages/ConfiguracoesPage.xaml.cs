using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

public partial class ConfiguracoesPage : ContentPage
{
    private const string UrlGerarChave = "https://aistudio.google.com/apikey";

    private readonly IConfiguracaoService _configuracao;

    public ConfiguracoesPage(IConfiguracaoService configuracao)
    {
        _configuracao = configuracao;

        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await AtualizarStatusAsync();
    }

    private async Task AtualizarStatusAsync()
    {
        var chave = await _configuracao.ObterChaveGeminiAsync();
        bool temChave = !string.IsNullOrWhiteSpace(chave);

        StatusLabel.Text = temChave
            ? "Há uma chave salva neste dispositivo."
            : "Nenhuma chave salva. A narração e a tradução ficam desativadas.";

        StatusLabel.TextColor = temChave ? Colors.Green : Colors.Gray;
        RemoverButton.IsVisible = temChave;
    }

    private async void OnSalvarClicked(object sender, EventArgs e)
    {
        var chave = ChaveEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(chave))
        {
            await DisplayAlert("Chave vazia", "Cole a sua chave da Gemini API no campo.", "OK");
            return;
        }

        try
        {
            await _configuracao.SalvarChaveGeminiAsync(chave);

            // Não mantém a chave visível na tela depois de salvar.
            ChaveEntry.Text = string.Empty;

            await AtualizarStatusAsync();

            await DisplayAlert("Chave salva", "Agora a narração e a tradução podem ser usadas.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Não foi possível salvar a chave", ex.Message, "OK");
        }
    }

    private async void OnRemoverClicked(object sender, EventArgs e)
    {
        bool confirmou = await DisplayAlert(
            "Remover chave",
            "Deseja remover a chave salva neste dispositivo?",
            "Remover",
            "Cancelar"
        );

        if (!confirmou)
            return;

        _configuracao.RemoverChaveGemini();

        await AtualizarStatusAsync();
    }

    private async void OnGerarChaveClicked(object sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(UrlGerarChave));
        }
        catch (Exception)
        {
            await DisplayAlert("Não foi possível abrir o link", UrlGerarChave, "OK");
        }
    }
}
