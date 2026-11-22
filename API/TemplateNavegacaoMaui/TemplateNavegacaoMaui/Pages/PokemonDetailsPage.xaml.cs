using System.Diagnostics;
using TemplateNavegacaoMaui.Models;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

public partial class PokemonDetailsPage : ContentPage, IQueryAttributable
{
    private readonly IGeminiService _geminiService;

    public PokemonDetailsPage(IGeminiService geminiService)
    {
        InitializeComponent();

        _geminiService = geminiService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("OnibusSelecionado", out var resultado) && resultado is Onibus onibus)
        {
            BindingContext = onibus;

            if (onibus.Pokemon is PokemonInfo pokemon)
            {
                // Exibe imediatamente a tradução existente ou o texto original.
                PokedexDescriptionLabel.Text = !string.IsNullOrWhiteSpace(
                    pokemon.PokedexDescriptionTraduzida
                )
                    ? pokemon.PokedexDescriptionTraduzida
                    : pokemon.PokedexDescription;

                _ = TraduzirDescricaoAsync(pokemon);
            }
        }
    }

    private async Task TraduzirDescricaoAsync(PokemonInfo pokemon)
    {
        string descricaoOriginal = pokemon.PokedexDescription ?? string.Empty;

        // Não inicia a tradução se já houver uma.
        if (!string.IsNullOrWhiteSpace(pokemon.PokedexDescriptionTraduzida))
            return;

        if (string.IsNullOrWhiteSpace(descricaoOriginal))
        {
            PokedexDescriptionLabel.Text = "Descrição da Pokédex indisponível.";
            return;
        }

        // Guarda o texto original antes de iniciar a chamada.
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PokedexDescriptionLabel.Text = descricaoOriginal;
            TraducaoStatusLabel.Text = "Gemini está traduzindo a descrição... Por favor, aguarde.";
            TraducaoStatusLabel.IsVisible = true;
        });

        Debug.WriteLine(
            $"[PokéBus] Descrição original recebida: {descricaoOriginal.Length} caracteres."
        );

        try
        {
            string traducao = await _geminiService.TraduzirDescricaoPokedexAsync(descricaoOriginal);

            Debug.WriteLine($"[PokéBus] Tradução recebida: {traducao?.Length ?? 0} caracteres.");

            Debug.WriteLine($"[PokéBus] Resultado: {traducao}");

            if (string.IsNullOrWhiteSpace(traducao))
            {
                throw new InvalidOperationException("O Gemini retornou uma tradução vazia.");
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PokedexDescriptionLabel.Text = traducao.Trim();
                PokedexDescriptionLabel.IsVisible = true;

                TraducaoStatusLabel.IsVisible = false;
            });
        }
        catch (ChaveGeminiAusenteException)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PokedexDescriptionLabel.Text = descricaoOriginal;

                TraducaoStatusLabel.Text =
                    "Para traduzir a descrição, informe a sua chave da Gemini em Configurações.";
                TraducaoStatusLabel.IsVisible = true;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PokéBus] ERRO NA TRADUÇÃO: {ex}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Nunca deixa a descrição desaparecer.
                PokedexDescriptionLabel.Text = descricaoOriginal;

                TraducaoStatusLabel.Text =
                    "Não foi possível traduzir. Exibindo descrição original.";

                // Mantém a mensagem de erro visível.
                TraducaoStatusLabel.IsVisible = true;
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
