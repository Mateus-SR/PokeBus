
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using TemplateNavegacaoMaui.Models;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui.Pages;

public partial class MainPage : ContentPage
{
    private readonly RadarService _radarService =
        new(new HttpClient());

    private readonly PokemonService _pokemonService =
        new(new HttpClient());

    private bool _jaCarregou;

    public ObservableCollection<Onibus> Onibus { get; } = [];

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_jaCarregou)
        {
            _jaCarregou = true;
            await CarregarOnibusAsync();
        }
    }

    private async void OnAtualizarClicked(object sender, EventArgs e)
    {
        await CarregarOnibusAsync();
    }

    private async Task CarregarOnibusAsync()
    {
        AtualizarButton.IsEnabled = false;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        StatusLabel.Text = "Consultando a API...";

        Onibus.Clear();

        try
        {
            // 1. Consulta o radar de ônibus.

            var resultado = await _radarService.ObterOnibusAsync();

            foreach (var onibus in resultado)
                Onibus.Add(onibus);

            if (resultado.Count == 0)
            {
                StatusLabel.Text = "Nenhum ônibus encontrado.";
                return;
            }

            StatusLabel.Text =
                $"{resultado.Count} ônibus encontrados. Consultando Pokémon...";

            // 2. Consulta os Pokémon sem bloquear a exibição dos ônibus.

            await Task.WhenAll(
                resultado.Select(AssociarPokemonAsync));

            StatusLabel.Text =
                $"{resultado.Count} ônibus encontrados.";
        }
        catch (HttpRequestException)
        {
            StatusLabel.Text =
                "Não foi possível conectar à API do radar.";
        }
        catch (System.Text.Json.JsonException)
        {
            StatusLabel.Text =
                "A resposta da API não está no formato esperado.";
        }
        catch (TaskCanceledException)
        {
            StatusLabel.Text =
                "A consulta demorou demais. Tente novamente.";
        }
        catch (Exception)
        {
            StatusLabel.Text =
                "Ocorreu um erro inesperado.";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            AtualizarButton.IsEnabled = true;
        }
    }

    private async Task AssociarPokemonAsync(Onibus onibus)
    {
        // Extrai os três últimos dígitos do ID.

        var match = Regex.Match(onibus.Id ?? "", @"(\d{3})$");

        if (!match.Success)
            return;

        if (!int.TryParse(match.Value, out int pokemonId))
            return;

        // Consulta a PokeAPI.

        var pokemon = await _pokemonService.ObterPokemonAsync(pokemonId);

        // Associa o resultado ao ônibus.

        onibus.Pokemon = pokemon;
    }

    private async void OnOnibusTapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Frame frame ||
            frame.BindingContext is not Onibus onibus)
            return;

        if (onibus.Pokemon == null)
        {
            await DisplayAlert(
                "Pokémon indisponível",
                "Não foi possível carregar os dados deste Pokémon. Tente atualizar o radar.",
                "OK");

            return;
        }

        // Abre a página de detalhes na pilha de navegação.

        await Shell.Current.GoToAsync(
            nameof(PokemonDetailsPage),
            new Dictionary<string, object>
            {
                ["OnibusSelecionado"] = onibus
            });
    }
}
