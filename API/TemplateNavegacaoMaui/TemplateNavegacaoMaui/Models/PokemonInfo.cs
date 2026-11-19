
namespace TemplateNavegacaoMaui.Models;

public sealed class PokemonInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpriteUrl { get; set; } = string.Empty;

    public string HeightText { get; set; } = string.Empty;
    public string WeightText { get; set; } = string.Empty;
    public string TypesText { get; set; } = string.Empty;
    public string AbilitiesText { get; set; } = string.Empty;
    public string BaseExperienceText { get; set; } = string.Empty;
    public string StatsText { get; set; } = string.Empty;

    // Tipos em formato utilizável pelo sistema de batalha.
    public List<string> Types { get; set; } = [];

    // Atributos numéricos recebidos da PokeAPI.
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }
}
