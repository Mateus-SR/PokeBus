
using TemplateNavegacaoMaui.Models;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

[QueryProperty(nameof(Batalha), "Batalha")]
public partial class BatalhaPage : ContentPage
{
    private readonly GeminiService _geminiService = new();

    private BatalhaPokemon? _batalha;

    public BatalhaPokemon? Batalha
    {
        get => _batalha;
        set
        {
            _batalha = value;

            if (_batalha != null)
                _ = ExibirBatalhaAsync();
        }
    }

    public BatalhaPage()
    {
        InitializeComponent();
    }

    private async Task ExibirBatalhaAsync()
    {
        if (_batalha == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PokemonAImage.Source = _batalha.ParticipanteA.Pokemon.SpriteUrl;
            PokemonBImage.Source = _batalha.ParticipanteB.Pokemon.SpriteUrl;

            PokemonALabel.Text = _batalha.ParticipanteA.Pokemon.Name;
            PokemonBLabel.Text = _batalha.ParticipanteB.Pokemon.Name;

            LinhaALabel.Text =
                $"Linha {_batalha.ParticipanteA.Onibus.CodigoLetreiro}";

            LinhaBLabel.Text =
                $"Linha {_batalha.ParticipanteB.Onibus.CodigoLetreiro}";

            HpALabel.Text =
                $"HP: {_batalha.ParticipanteA.HpAtual}/{_batalha.ParticipanteA.HpMaximo}";

            HpBLabel.Text =
                $"HP: {_batalha.ParticipanteB.HpAtual}/{_batalha.ParticipanteB.HpMaximo}";

            ResultadoLabel.Text = _batalha.Empate
                ? "EMPATE!"
                : $"VENCEDOR: {_batalha.Vencedor?.Pokemon.Name}";

            StatusLabel.Text = "O narrador está preparando a batalha...";
            NarracaoLabel.Text = string.Empty;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
        });

        try
        {
            var narracao = await _geminiService.NarrarBatalhaAsync(
                _batalha.Relatorio
            );

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "Batalha concluída!";
                NarracaoLabel.Text = narracao;

                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = "Não foi possível gerar a narração.";

                NarracaoLabel.Text =
                    "O resultado foi calculado normalmente, mas houve um erro na narração.\n\n" +
                    ex.Message + "\n\n" +
                    _batalha.Relatorio;

                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            });
        }
    }
}
