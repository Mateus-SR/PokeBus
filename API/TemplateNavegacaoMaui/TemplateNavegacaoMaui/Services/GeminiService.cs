using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TemplateNavegacaoMaui.Services;

public sealed class GeminiService
{
    private const string ApiUrl =
        "https://generativelanguage.googleapis.com/v1beta/interactions?alt=sse";

    private const string Model = "gemini-3.1-flash-lite";

    private const string PausaTurno = "[[PAUSA_TURNO]]";
    private const string PausaDramatica = "[[PAUSA_DRAMATICA]]";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
    }

    public GeminiService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
    }

    public async Task NarrarBatalhaEmStreamingAsync(
        string relatorio,
        Func<string, Task> aoReceberTexto,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "A variável de ambiente GEMINI_API_KEY não foi configurada."
            );
        }

        if (string.IsNullOrWhiteSpace(relatorio))
        {
            throw new ArgumentException("O relatório da batalha está vazio.", nameof(relatorio));
        }

        if (aoReceberTexto == null)
        {
            throw new ArgumentNullException(nameof(aoReceberTexto));
        }

        var requestBody = new
        {
            model = Model,
            stream = true,

            system_instruction = $$"""
                Você é um narrador de batalhas Pokémon no estilo dos jogos clássicos.

                Narre batalhas entre Pokémon associados a ônibus. Imagine que os ônibus são os treinadores e que os Pokémon os representam.
                Sua narração deve ser empolgante, dramática e divertida, como um episódio de anime Pokémon.
                Sempre considere o nome sentido do ônibus e use isso para criar uma narrativa envolvente.

                REGRAS OBRIGATÓRIAS:
                - Não use estilização Markdown. Use sempre texto plano.
                - Utilize exclusivamente os seguintes marcadores de formatação:
                    [[POKEMON]]nome do Pokémon[[/POKEMON]]
                    [[HP]]quantidade de HP[[/HP]], sempre incluindo a palavra "HP" após do valor, como em "50 de HP".
                    [[GOLPE]]nome do golpe[[/GOLPE]]
                    [[ONIBUS]]linha e sentido do ônibus[[/ONIBUS]]

                - Envolva todos os nomes de Pokémon com [[POKEMON]] e [[/POKEMON]].
                - Envolva todos os valores de HP com [[HP]] e [[/HP]].
                - Envolva todos os nomes de golpes com [[GOLPE]] e [[/GOLPE]].
                - Envolva os nomes das linhas de ônibus e seus respectivos sentidos com [[ONIBUS]] e [[/ONIBUS]].
                - Não coloque marcadores dentro de outros marcadores.
                - Não altere o conteúdo dos nomes, valores ou informações ao adicionar os marcadores.
                - Nunca escreva os marcadores de formatação literalmente na narração.
                - Narre em português brasileiro.
                - Respeite integralmente os acontecimentos do relatório.
                - Não invente ataques, danos, resultados ou acontecimentos.
                - Sempre use os ataques informados no relatório, sem inventar outros..
                - Não altere os valores de HP.
                - Não mude o vencedor.
                - Valorize os momentos decisivos da batalha.
                - Use diálogos e exclamações com moderação.
                - Produza uma narração dividida por turnos. Multiplas linhas por turno são permitidas, seguindo o padrão:
                    [Turno X]: [Ação do Pokémon A] [Resultado da ação do Pokémon A] [Ação do Pokémon B] [Resultado da ação do Pokémon B]
                - Ao terminar cada turno, insira exatamente um dos marcadores abaixo, sem explicá-los:
                  {{PausaTurno}} para turnos normais.
                  {{PausaDramatica}} para turnos com momentos decisivos, como uma derrota ou o golpe final.
                - Não insira marcadores de pausa durante o diálogo de apresentação, antes do primeiro turno.
                - Não insira marcadores depois da declaração final do vencedor.
                - Os marcadores devem ficar entre os turnos, nunca no meio de uma ação.
                - Não escreva os marcadores em nenhum outro lugar.
                - Termine com uma declaração emocionante do vencedor, mencionando a linha do ônibus e o nome inteiro dele.
                """,

            input = relatorio,

            generation_config = new { max_output_tokens = 1800 },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);

        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Erro na Gemini API: {(int)response.StatusCode} "
                    + $"{response.StatusCode}. Resposta: {erro}"
            );
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(stream);

        var buffer = new StringBuilder();
        var dadosEvento = new StringBuilder();

        bool recebeuTexto = false;

        async Task ProcessarBufferAsync(bool finalizar)
        {
            while (true)
            {
                string? marcadorEncontrado = null;
                int indiceMarcador = -1;

                int indiceTurno = buffer.ToString().IndexOf(PausaTurno, StringComparison.Ordinal);

                int indiceDramatico = buffer
                    .ToString()
                    .IndexOf(PausaDramatica, StringComparison.Ordinal);

                if (indiceTurno >= 0 && (indiceDramatico < 0 || indiceTurno < indiceDramatico))
                {
                    indiceMarcador = indiceTurno;
                    marcadorEncontrado = PausaTurno;
                }
                else if (indiceDramatico >= 0)
                {
                    indiceMarcador = indiceDramatico;
                    marcadorEncontrado = PausaDramatica;
                }

                if (indiceMarcador >= 0 && marcadorEncontrado != null)
                {
                    string texto = buffer.ToString(0, indiceMarcador);

                    if (!string.IsNullOrEmpty(texto))
                    {
                        await aoReceberTexto(texto);
                        recebeuTexto = true;
                    }

                    buffer.Remove(0, indiceMarcador + marcadorEncontrado.Length);

                    int pausa = marcadorEncontrado == PausaDramatica ? 3000 : 2000;

                    await Task.Delay(pausa, cancellationToken);

                    continue;
                }

                if (finalizar)
                {
                    if (buffer.Length > 0)
                    {
                        await aoReceberTexto(buffer.ToString());
                        recebeuTexto = true;
                        buffer.Clear();
                    }

                    break;
                }

                // Preserva o final do buffer para detectar marcadores
                // que tenham sido divididos entre eventos do streaming.
                string conteudo = buffer.ToString();

                int preservar = 0;

                int limite = Math.Min(
                    Math.Max(PausaTurno.Length, PausaDramatica.Length) - 1,
                    conteudo.Length
                );

                // Preserva somente os caracteres que podem ser o início
                // de um marcador dividido entre eventos.
                for (int tamanho = limite; tamanho > 0; tamanho--)
                {
                    string final = conteudo.Substring(conteudo.Length - tamanho, tamanho);

                    bool podeSerMarcador =
                        PausaTurno.StartsWith(final, StringComparison.Ordinal)
                        || PausaDramatica.StartsWith(final, StringComparison.Ordinal);

                    if (podeSerMarcador)
                    {
                        preservar = tamanho;
                        break;
                    }
                }

                int quantidade = buffer.Length - preservar;

                if (quantidade > 0)
                {
                    string texto = buffer.ToString(0, quantidade);

                    await aoReceberTexto(texto);
                    recebeuTexto = true;

                    buffer.Remove(0, quantidade);
                }

                break;
            }
        }

        async Task ProcessarEventoAsync(string dados)
        {
            if (string.IsNullOrWhiteSpace(dados))
                return;

            dados = dados.Trim();

            // Ignora sinalizações de encerramento que não são JSON.
            if (
                dados.Equals("DONE", StringComparison.OrdinalIgnoreCase)
                || dados.Equals("[DONE]", StringComparison.OrdinalIgnoreCase)
            )
            {
                return;
            }

            // Alguns eventos SSE podem não conter objetos JSON.
            if (!dados.StartsWith("{", StringComparison.Ordinal))
                return;

            using var documento = JsonDocument.Parse(dados);

            var raiz = documento.RootElement;

            if (!raiz.TryGetProperty("delta", out var delta))
                return;

            if (!delta.TryGetProperty("type", out var tipo))
                return;

            if (tipo.GetString() != "text")
                return;

            if (!delta.TryGetProperty("text", out var textoElemento))
                return;

            string? texto = textoElemento.GetString();

            if (string.IsNullOrEmpty(texto))
                return;

            buffer.Append(texto);

            await ProcessarBufferAsync(false);
        }

        string? linha;

        while ((linha = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (linha.Length == 0)
            {
                if (dadosEvento.Length > 0)
                {
                    await ProcessarEventoAsync(dadosEvento.ToString());
                    dadosEvento.Clear();
                }

                continue;
            }

            if (linha.StartsWith("data:", StringComparison.Ordinal))
            {
                if (dadosEvento.Length > 0)
                    dadosEvento.Append('\n');

                dadosEvento.Append(linha.Substring(5).TrimStart());
            }
        }

        // Processa o último evento, caso o fluxo termine sem linha vazia.
        if (dadosEvento.Length > 0)
        {
            await ProcessarEventoAsync(dadosEvento.ToString());
        }

        await ProcessarBufferAsync(true);

        if (!recebeuTexto)
        {
            throw new InvalidOperationException("O Gemini não retornou uma narração válida.");
        }
    }
}
