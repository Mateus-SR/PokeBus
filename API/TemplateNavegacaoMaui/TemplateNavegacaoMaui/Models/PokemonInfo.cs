using System.ComponentModel;

namespace TemplateNavegacaoMaui.Models;

public sealed class PokemonInfo : INotifyPropertyChanged
{
    private string _pokedexDescriptionTraduzida = string.Empty;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpriteUrl { get; set; } = string.Empty;
    public string BackSpriteUrl { get; set; } = string.Empty;

    // Descrição original recebida da PokeAPI.
    public string PokedexDescription { get; set; } = string.Empty;

    // Tradução fornecida pelo Gemini.
    public string PokedexDescriptionTraduzida
    {
        get => _pokedexDescriptionTraduzida;
        set
        {
            if (_pokedexDescriptionTraduzida == value)
                return;

            _pokedexDescriptionTraduzida = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(PokedexDescriptionTraduzida))
            );

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(PokedexDescriptionExibicao))
            );
        }
    }

    // Exibe a tradução quando disponível; caso contrário, usa o original.
    public string PokedexDescriptionExibicao =>
        string.IsNullOrWhiteSpace(PokedexDescriptionTraduzida)
            ? PokedexDescription
            : PokedexDescriptionTraduzida;

    public string HeightText { get; set; } = string.Empty;
    public string WeightText { get; set; } = string.Empty;
    public string TypesText { get; set; } = string.Empty;
    public string AbilitiesText { get; set; } = string.Empty;
    public string BaseExperienceText { get; set; } = string.Empty;
    public string StatsText { get; set; } = string.Empty;

    // Tipos em formato utilizável pelo sistema de batalha.
    public List<string> Types { get; set; } = [];

    // Todos os golpes disponibilizados pela PokeAPI.
    public List<GolpePokemon> Golpes { get; set; } = [];

    // Atributos numéricos recebidos da PokeAPI.
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
