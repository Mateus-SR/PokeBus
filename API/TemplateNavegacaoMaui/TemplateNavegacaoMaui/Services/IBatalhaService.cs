using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Simula uma batalha entre os Pokémon associados a dois ônibus.
/// </summary>
public interface IBatalhaService
{
    BatalhaPokemon SimularBatalha(Onibus onibusA, Onibus onibusB);
}
