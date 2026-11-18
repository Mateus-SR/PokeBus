
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TemplateNavegacaoMaui.Models;

public sealed class RadarResponse
{
    [JsonPropertyName("onibus")]
    public List<Onibus> Onibus { get; set; } = [];
}

public sealed class Onibus : INotifyPropertyChanged
{
    private PokemonInfo? _pokemon;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("codigoLetreiro")]
    public string CodigoLetreiro { get; set; } = string.Empty;

    [JsonPropertyName("sentidoLinha")]
    public string SentidoLinha { get; set; } = string.Empty;

    [JsonPropertyName("previsao")]
    public string Previsao { get; set; } = string.Empty;

    public PokemonInfo? Pokemon
    {
        get => _pokemon;
        set
        {
            if (_pokemon == value)
                return;

            _pokemon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PokemonSpriteUrl));
        }
    }

    public string? PokemonSpriteUrl => Pokemon?.SpriteUrl;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
