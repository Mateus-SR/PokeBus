using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Obtém os dados de um Pokémon (e seus golpes) a partir do número da Pokédex.
/// </summary>
public interface IPokemonService
{
    /// <returns>O Pokémon, ou <c>null</c> se o id for inválido ou a consulta falhar.</returns>
    Task<PokemonInfo?> ObterPokemonAsync(int id);
}
