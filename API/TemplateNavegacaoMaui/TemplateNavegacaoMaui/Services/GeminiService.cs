
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TemplateNavegacaoMaui.Services;

public sealed class GeminiService
{
    private const string ApiUrl =
        "https://generativelanguage.googleapis.com/v1beta/interactions";

    private const string Model = "gemini-3.1-flash-lite";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(90)
        };

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? string.Empty;
    }

    public GeminiService(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? string.Empty;
    }

    public async Task<string> NarrarBatalhaAsync(
        string relatorio,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "A variável de ambiente GEMINI_API_KEY não foi configurada."
            );
        }

        if (string.IsNullOrWhiteSpace(relatorio))
        {
            throw new ArgumentException(
                "O relatório da batalha está vazio.",
                nameof(relatorio)
            );
        }

        var requestBody = new
        {
            model = Model,

            system_instruction =
                """
                Você é um narrador de batalhas Pokémon no estilo dos jogos clássicos.

                Narre batalhas entre Pokémon associados a ônibus. Imagine que os ônibus são os treinadores e que os Pokémons os representam.
                Sua narração deve ser empolgante, dramática e divertida,
                como um episódio de anime Pokémon.

                REGRAS OBRIGATÓRIAS:
                - Não use estilização markdown: o ambiente que receberá os textos não aceitará eles. Use sempre texto plano.
                - Narre em português brasileiro.
                - Respeite integralmente os acontecimentos do relatório.
                - Não invente ataques, danos, resultados ou acontecimentos.
                - Não altere os valores de HP.
                - Não mude o vencedor.
                - Valorize os momentos decisivos da batalha.
                - Use diálogos e exclamações com moderação.
                - Produza uma narração dividida por turnos.
                - Termine com uma declaração emocionante do vencedor, mencionando a linha do onibus e o nome inteiro dele.
                """,

            input = relatorio,

            generation_config = new
            {
                max_output_tokens = 1800
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            ApiUrl
        );

        request.Headers.Add("x-goog-api-key", _apiKey);

        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken
        );

        var responseContent = await response.Content.ReadAsStringAsync(
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Erro na Gemini API: {(int)response.StatusCode} " +
                $"{response.StatusCode}. Resposta: {responseContent}"
            );
        }

        using var document = JsonDocument.Parse(responseContent);

        var root = document.RootElement;

        if (!root.TryGetProperty("steps", out var steps))
        {
            throw new InvalidOperationException(
                "A resposta do Gemini não contém etapas de saída."
            );
        }

        var textos = steps
            .EnumerateArray()
            .Where(step =>
                step.TryGetProperty("type", out var type) &&
                type.GetString() == "model_output"
            )
            .Where(step => step.TryGetProperty("content", out _))
            .SelectMany(step =>
                step.GetProperty("content").EnumerateArray()
            )
            .Where(content =>
                content.TryGetProperty("type", out var type) &&
                type.GetString() == "text" &&
                content.TryGetProperty("text", out _)
            )
            .Select(content =>
                content.GetProperty("text").GetString()
            )
            .Where(text => !string.IsNullOrWhiteSpace(text));

        var narracao = string.Join(
            Environment.NewLine + Environment.NewLine,
            textos
        );

        if (string.IsNullOrWhiteSpace(narracao))
        {
            throw new InvalidOperationException(
                "O Gemini não retornou uma narração válida."
            );
        }

        return narracao;
    }
}
