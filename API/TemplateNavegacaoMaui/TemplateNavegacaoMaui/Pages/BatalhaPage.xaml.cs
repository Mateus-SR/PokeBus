using TemplateNavegacaoMaui.Models;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

[QueryProperty(nameof(Batalha), "Batalha")]
public partial class BatalhaPage : ContentPage
{
    private readonly GeminiService _geminiService = new();
    private readonly BatalhaService _batalhaService = new();

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

    private async void OnSortearNovamenteClicked(object sender, EventArgs e)
    {
        SortearNovamenteButton.IsEnabled = false;

        try
        {
            // Obtém os ônibus que possuem Pokémon carregados.
            var disponiveis = OnibusStore.Onibus.Where(onibus => onibus.Pokemon != null).ToList();

            if (disponiveis.Count < 2)
            {
                await DisplayAlert(
                    "Pokémon insuficientes",
                    "É necessário ter pelo menos dois ônibus com Pokémon carregados.",
                    "OK"
                );

                return;
            }

            // Sorteia dois ônibus diferentes.
            int indiceA = Random.Shared.Next(disponiveis.Count);

            int indiceB;

            do
            {
                indiceB = Random.Shared.Next(disponiveis.Count);
            } while (indiceA == indiceB);

            var onibusA = disponiveis[indiceA];
            var onibusB = disponiveis[indiceB];

            // Calcula a nova batalha.
            _batalha = _batalhaService.SimularBatalha(onibusA, onibusB);

            // Atualiza a página e solicita uma nova narração.
            await ExibirBatalhaAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro ao sortear batalha", ex.Message, "OK");
        }
        finally
        {
            SortearNovamenteButton.IsEnabled = true;
        }
    }

    public BatalhaPage()
    {
        InitializeComponent();
    }

    private async void OnTentarNovamenteClicked(object sender, EventArgs e)
    {
        await ExibirBatalhaAsync();
    }

    private async Task ExibirBatalhaAsync()
    {
        if (_batalha == null)
            return;

        TentarNovamenteButton.IsEnabled = false;
        TentarNovamenteButton.IsVisible = false;
        SortearNovamenteButton.IsVisible = false;
        SortearNovamenteButton.IsEnabled = false;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            StatusLabel.Text = "O narrador está preparando a batalha...";
            NarracaoLabel.Text = string.Empty;

            PokemonAImage.Url = _batalha.ParticipanteA.Pokemon.SpriteUrl;
            PokemonBImage.Url = _batalha.ParticipanteB.Pokemon.SpriteUrl;

            PokemonALabel.Text = _batalha.ParticipanteA.Pokemon.Name;
            PokemonBLabel.Text = _batalha.ParticipanteB.Pokemon.Name;

            LinhaALabel.Text = $"Linha {_batalha.ParticipanteA.Onibus.CodigoLetreiro}";

            SentidoALabel.Text = $"Sentido {_batalha.ParticipanteA.Onibus.SentidoLinha}";

            LinhaBLabel.Text = $"Linha {_batalha.ParticipanteB.Onibus.CodigoLetreiro}";

            SentidoBLabel.Text = $"Sentido {_batalha.ParticipanteB.Onibus.SentidoLinha}";

            HpALabel.Text =
                $"HP: {_batalha.ParticipanteA.HpAtual}/{_batalha.ParticipanteA.HpMaximo}";

            HpBLabel.Text =
                $"HP: {_batalha.ParticipanteB.HpAtual}/{_batalha.ParticipanteB.HpMaximo}";

            ResultadoLabel.Text = _batalha.Empate
                ? "EMPATE!"
                : $"VENCEDOR: {_batalha.Vencedor?.Pokemon.Name}";

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
        });

        try
        {
            StatusLabel.Text = "A batalha começou!";

            await _geminiService.NarrarBatalhaEmStreamingAsync(_batalha.Relatorio, async trecho =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    NarracaoLabel.Text += trecho;
                });
            });

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = "Batalha concluída!";
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                StatusLabel.Text = "Não foi possível gerar a narração.";

                NarracaoLabel.Text =
                    "O resultado foi calculado normalmente, mas houve um erro na narração.\n\n"
                    + ex.Message
                    + "\n\n"
                    + _batalha.Relatorio;

                TentarNovamenteButton.IsVisible = true;
                TentarNovamenteButton.IsEnabled = true;
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;

                SortearNovamenteButton.IsVisible = true;
                SortearNovamenteButton.IsEnabled = true;
            });
        }
    }
}
