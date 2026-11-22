using System.Net.Http.Json;
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

public sealed class RadarService : IRadarService
{
    private const string Endpoint =
        "https://busca-bus-server.onrender.com/v1/exibicoes/2aTN6E/radar"; // Endpoint usado no PI do 4º Semestre. Ele faz uma busca direta na API OlhoVivo da SPTrans

    private readonly HttpClient _httpClient;

    public RadarService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Onibus>> ObterOnibusAsync(
        CancellationToken cancellationToken = default)
    {
        var resposta = await _httpClient.GetFromJsonAsync<RadarResponse>(
            Endpoint, cancellationToken);

        return resposta?.Onibus ?? []; // Retorna bloco json contendo diversas informações vindas da SPTrans e tratadas no backend do Busca Bus
    }
}
