using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Consulta os ônibus em circulação (radar do Busca Bus / Olho Vivo da SPTrans).
/// </summary>
public interface IRadarService
{
    Task<IReadOnlyList<Onibus>> ObterOnibusAsync(CancellationToken cancellationToken = default);
}
