using System.Text;
using TemplateNavegacaoMaui.Models;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

[QueryProperty(nameof(Batalha), "Batalha")]
public partial class BatalhaPage : ContentPage
{
    private readonly IGeminiService _geminiService;
    private readonly IBatalhaService _batalhaService;
    private readonly OnibusStore _onibusStore;
    private FormattedString _narracaoFormatada = new();
    private readonly StringBuilder _bufferFormatacao = new();

    private static readonly (string Texto, string Estilo, bool Abrir)[] Marcadores =
    [
        ("[[POKEMON]]", "POKEMON", true),
        ("[[/POKEMON]]", "POKEMON", false),
        ("[[HP]]", "HP", true),
        ("[[/HP]]", "HP", false),
        ("[[GOLPE]]", "GOLPE", true),
        ("[[/GOLPE]]", "GOLPE", false),
        ("[[ONIBUS]]", "ONIBUS", true),
        ("[[/ONIBUS]]", "ONIBUS", false),
    ];

    private void ProcessarTextoFormatado(string trecho, bool finalizar = false)
    {
        _bufferFormatacao.Append(trecho);

        while (true)
        {
            string conteudo = _bufferFormatacao.ToString();

            int indiceMarcador = -1;
            (string Texto, string Estilo, bool Abrir) marcadorEncontrado = default;

            foreach (var marcador in Marcadores)
            {
                int indice = conteudo.IndexOf(marcador.Texto, StringComparison.Ordinal);

                if (indice >= 0 && (indiceMarcador < 0 || indice < indiceMarcador))
                {
                    indiceMarcador = indice;
                    marcadorEncontrado = marcador;
                }
            }

            if (indiceMarcador >= 0)
            {
                if (indiceMarcador > 0)
                {
                    AdicionarTextoFormatado(conteudo.Substring(0, indiceMarcador));
                }

                _bufferFormatacao.Remove(0, indiceMarcador + marcadorEncontrado.Texto.Length);

                _estiloAtual = marcadorEncontrado.Abrir ? marcadorEncontrado.Estilo : "NORMAL";

                continue;
            }

            if (finalizar)
            {
                if (_bufferFormatacao.Length > 0)
                {
                    AdicionarTextoFormatado(_bufferFormatacao.ToString());
                    _bufferFormatacao.Clear();
                }

                _estiloAtual = "NORMAL";
                break;
            }

            // Mantém o possível início de um marcador dividido no streaming.
            int preservar = 0;
            int limite = Math.Min(Marcadores.Max(m => m.Texto.Length) - 1, conteudo.Length);

            for (int tamanho = limite; tamanho > 0; tamanho--)
            {
                string final = conteudo.Substring(conteudo.Length - tamanho, tamanho);

                if (Marcadores.Any(m => m.Texto.StartsWith(final, StringComparison.Ordinal)))
                {
                    preservar = tamanho;
                    break;
                }
            }

            int quantidade = _bufferFormatacao.Length - preservar;

            if (quantidade > 0)
            {
                AdicionarTextoFormatado(_bufferFormatacao.ToString(0, quantidade));

                _bufferFormatacao.Remove(0, quantidade);
            }

            break;
        }
    }

    private void AdicionarTextoFormatado(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return;

        var span = new Span
        {
            Text = texto,
            FontFamily = "PokemonEmerald",
            FontSize = 30,
            TextColor = Colors.Black,
        };

        switch (_estiloAtual)
        {
            case "POKEMON":
                span.FontAttributes = FontAttributes.Bold;
                break;

            case "HP":
                if (
                    Application.Current?.Resources.TryGetValue("PrimaryColor", out var cor) == true
                    && cor is Color corPrimaria
                )
                {
                    span.TextColor = corPrimaria;
                }
                else
                {
                    span.TextColor = Colors.Red;
                }

                break;

            case "GOLPE":
            case "ONIBUS":
                span.TextColor = Color.FromArgb("#1515f0");
                break;
        }

        _narracaoFormatada.Spans.Add(span);
    }

    private string _estiloAtual = "NORMAL";

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
            var disponiveis = _onibusStore.Onibus.Where(onibus => onibus.Pokemon != null).ToList();

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

    public BatalhaPage(
        IGeminiService geminiService,
        IBatalhaService batalhaService,
        OnibusStore onibusStore)
    {
        _geminiService = geminiService;
        _batalhaService = batalhaService;
        _onibusStore = onibusStore;

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
            _bufferFormatacao.Clear();
            _estiloAtual = "NORMAL";

            _narracaoFormatada = new FormattedString();
            NarracaoLabel.FormattedText = _narracaoFormatada;

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
                    ProcessarTextoFormatado(trecho);
                });
            });

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProcessarTextoFormatado(string.Empty, finalizar: true);
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
                NarracaoLabel.FormattedText = null;
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

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
