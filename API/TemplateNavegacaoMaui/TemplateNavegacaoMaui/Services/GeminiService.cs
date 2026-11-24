using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TemplateNavegacaoMaui.Services;

public sealed class GeminiService : IGeminiService
{
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/interactions";

    private static readonly string[] Modelos =
    [
        "gemini-3.8-flash",
        "gemini-3.5-flash-lite",
        "gemini-3.1-flash-lite",
    ];

    private const int MaxTentativasPorModelo = 3;
    private static readonly TimeSpan TimeoutOperacao = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan EsperaMaximaRetry = TimeSpan.FromSeconds(30);

    private static readonly ConcurrentDictionary<string, string> CacheTraducoes = new();

    private const string PausaTurno = "[[PAUSA_TURNO]]";
    private const string PausaDramatica = "[[PAUSA_DRAMATICA]]";

    private const string InstrucaoTraducao = """
        Você é um tradutor especializado em descrições da Pokédex.

        Traduza o texto recebido para português brasileiro natural.

        Regras:
        - Preserve integralmente o significado original.
        - Não acrescente nem remova informações.
        - Preserve os nomes oficiais dos Pokémon.
        - Preserve números, medidas e nomes próprios.
        - Não transforme a descrição em uma narrativa.
        - Não resuma nem simplifique o conteúdo.
        - Retorne somente o texto traduzido.
        - Não utilize Markdown, títulos ou comentários.
        """;

    private const string InstrucaoNarracao = """
        Você é um narrador de batalhas Pokémon no estilo dos jogos clássicos.

        Narre batalhas entre Pokémon associados a ônibus. Imagine que os ônibus são os treinadores e que os Pokémon os representam.
        Sua narração deve ser empolgante, dramática e divertida, como um episódio de anime Pokémon.
        Sempre considere o nome e o sentido do ônibus para criar uma narrativa envolvente.

        REGRAS DE FORMATAÇÃO:

        - Não utilize Markdown, HTML ou qualquer outra forma de estilização.
        - Utilize exclusivamente os marcadores personalizados definidos abaixo.
        - Os marcadores devem ser reproduzidos EXATAMENTE como estão escritos.
        - Nunca modifique, abrevie ou invente nomes de marcadores.
        - Todo marcador de abertura deve possuir seu respectivo marcador de fechamento.
        - O marcador de fechamento deve corresponder exatamente ao marcador de abertura.
        - Nunca utilize o nome do Pokémon, do golpe ou qualquer outra palavra como marcador de fechamento.
        - Não coloque marcadores dentro de outros marcadores.

        MARCADORES PERMITIDOS:

        1. Pokémon:
           [[POKEMON]]nome do Pokémon[[/POKEMON]]

        2. HP:
           [[HP]]quantidade de HP[[/HP]]

        3. Golpes:
           [[GOLPE]]nome do golpe[[/GOLPE]]

        4. Ônibus:
           [[ONIBUS]]linha e sentido do ônibus[[/ONIBUS]]

        REGRAS DE APLICAÇÃO:

        - Envolva TODAS as ocorrências dos nomes de Pokémon com os marcadores POKEMON, inclusive quando forem mencionados repetidamente.
        - Envolva TODAS as ocorrências dos valores de HP com os marcadores HP.
        - O texto de HP deve seguir um formato consistente, como "50 de HP".
        - Envolva TODAS as ocorrências dos nomes de golpes com os marcadores GOLPE.
        - Envolva os nomes das linhas de ônibus e seus respectivos sentidos com os marcadores ONIBUS.
        - Preserve integralmente a grafia original dos nomes próprios, golpes, linhas e sentidos.
        - Não adicione marcadores quando não houver informação correspondente no relatório.
        - Não deixe marcadores abertos ou incompletos.
        - Não escreva os marcadores literalmente fora de seus respectivos usos.

        EXEMPLOS DE FORMATAÇÃO CORRETA:

        [[POKEMON]]Dunsparce[[/POKEMON]] utiliza [[GOLPE]]Drill Run[[/GOLPE]]!

        O ataque causa [[HP]]50 de HP[[/HP]] de dano!

        [[ONIBUS]]Linha 875A — Sentido Terminal[[/ONIBUS]] comemora a vitória!

        EXEMPLOS DE FORMATAÇÃO PROIBIDA:

        Dunsparce[[/Dunsparce]]
        [[POKEMON]]Dunsparce[[/Pokemon]]
        [[POKEMON]]Dunsparce
        [[GOLPE]]Drill Run[[/POKEMON]]
        [[HP]]50[[/GOLPE]]

        REGRAS DE NARRAÇÃO:

        - Narre em português brasileiro.
        - Respeite integralmente os acontecimentos do relatório.
        - Não invente ataques, danos, resultados ou acontecimentos.
        - Sempre use os ataques informados no relatório, sem inventar outros.
        - Não altere os valores de HP.
        - Não mude o vencedor.
        - Valorize os momentos decisivos da batalha.
        - Use diálogos e exclamações com moderação.
        - Produza uma narração dividida por turnos.
        - Múltiplas linhas por turno são permitidas.
        - Siga o padrão:
          [Turno X]: [Ação do Pokémon A] [Resultado da ação do Pokémon A] [Ação do Pokémon B] [Resultado da ação do Pokémon B].
        - Considere a intensidade do golpe (HP perdido) para criar uma narrativa mais dramática e dinâmica.

        MARCADORES DE PAUSA:

        - Ao terminar cada turno, insira exatamente um dos marcadores abaixo, sem explicá-los:
          [[PAUSA_TURNO]] para turnos normais.
          [[PAUSA_DRAMATICA]] para turnos com momentos decisivos, como uma derrota ou o golpe final.
        - Não insira marcadores de pausa durante o diálogo de apresentação, antes do primeiro turno.
        - Não insira marcadores depois da declaração final do vencedor.
        - Os marcadores devem ficar entre os turnos, nunca no meio de uma ação.
        - Não escreva os marcadores de pausa em nenhum outro lugar.

        FINALIZAÇÃO:

        Termine com uma declaração emocionante do vencedor, mencionando a linha do ônibus e o nome inteiro dele.

        Antes de concluir, verifique se todos os marcadores de formatação estão corretamente abertos e fechados.
        """;

    private readonly HttpClient _httpClient;
    private readonly IConfiguracaoService _configuracao;

    public GeminiService(HttpClient httpClient, IConfiguracaoService configuracao)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuracao = configuracao ?? throw new ArgumentNullException(nameof(configuracao));
    }

    // A chave é lida a cada chamada: assim, salvar uma nova nas Configurações vale na hora.
    private async Task<string> ObterChaveObrigatoriaAsync()
    {
        var chave = await _configuracao.ObterChaveGeminiAsync();

        if (string.IsNullOrWhiteSpace(chave))
            throw new ChaveGeminiAusenteException();

        return chave;
    }

    public async Task<string> TraduzirDescricaoPokedexAsync(
        string descricao,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição está vazia.", nameof(descricao));

        if (CacheTraducoes.TryGetValue(descricao, out var traducaoEmCache))
            return traducaoEmCache;

        string apiKey = await ObterChaveObrigatoriaAsync();

        string traducao = await ExecutarComFallbackAsync(
            (modelo, token) => TraduzirComModeloAsync(modelo, descricao, apiKey, token),
            cancellationToken
        );

        CacheTraducoes.TryAdd(descricao, traducao);
        return CacheTraducoes[descricao];
    }

    public async Task NarrarBatalhaEmStreamingAsync(
        string relatorio,
        Func<string, Task> aoReceberTexto,
        CancellationToken cancellationToken = default,
        Func<Task>? aoReiniciar = null
    )
    {
        if (string.IsNullOrWhiteSpace(relatorio))
            throw new ArgumentException("O relatório da batalha está vazio.", nameof(relatorio));

        if (aoReceberTexto == null)
            throw new ArgumentNullException(nameof(aoReceberTexto));

        string apiKey = await ObterChaveObrigatoriaAsync();

        await ExecutarComFallbackAsync(
            async (modelo, token) =>
            {
                await NarrarComModeloEmStreamingAsync(
                    modelo,
                    relatorio,
                    apiKey,
                    aoReceberTexto,
                    token
                );

                return true;
            },
            cancellationToken,
            aoReiniciar
        );
    }

    private async Task<string> TraduzirComModeloAsync(
        string modelo,
        string descricao,
        string apiKey,
        CancellationToken cancellationToken
    )
    {
        var requestBody = CriarCorpoRequisicao(
            modelo,
            descricao,
            InstrucaoTraducao,
            stream: false,
            maxOutputTokens: 500
        );

        using var request = CriarRequest(requestBody, apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw await CriarExcecaoHttpAsync(response, "Erro na tradução Gemini", cancellationToken);

        var conteudo = await response.Content.ReadAsStringAsync(cancellationToken);

        using var documento = JsonDocument.Parse(conteudo);
        var raiz = documento.RootElement;

        if (!raiz.TryGetProperty("status", out var status) || status.GetString() != "completed")
            throw new InvalidOperationException("O Gemini não concluiu a tradução.");

        if (
            !raiz.TryGetProperty("steps", out var etapas)
            || etapas.ValueKind != JsonValueKind.Array
        )
        {
            throw new InvalidOperationException(
                "A resposta do Gemini não contém etapas de tradução."
            );
        }

        var resultado = new StringBuilder();

        foreach (var etapa in etapas.EnumerateArray())
        {
            if (
                !etapa.TryGetProperty("type", out var tipoEtapa)
                || tipoEtapa.GetString() != "model_output"
            )
            {
                continue;
            }

            if (
                !etapa.TryGetProperty("content", out var conteudos)
                || conteudos.ValueKind != JsonValueKind.Array
            )
            {
                continue;
            }

            foreach (var item in conteudos.EnumerateArray())
            {
                if (
                    item.TryGetProperty("type", out var tipoConteudo)
                    && tipoConteudo.GetString() == "text"
                    && item.TryGetProperty("text", out var texto)
                )
                {
                    resultado.Append(texto.GetString());
                }
            }
        }

        var traducao = resultado.ToString().Trim();

        if (string.IsNullOrWhiteSpace(traducao))
            throw new InvalidOperationException("O Gemini retornou uma tradução vazia.");

        return traducao;
    }

    private async Task NarrarComModeloEmStreamingAsync(
        string modelo,
        string relatorio,
        string apiKey,
        Func<string, Task> aoReceberTexto,
        CancellationToken cancellationToken
    )
    {
        var requestBody = CriarCorpoRequisicao(
            modelo,
            relatorio,
            InstrucaoNarracao,
            stream: true,
            maxOutputTokens: 1800
        );

        using var request = CriarRequest(requestBody, apiKey);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
            throw await CriarExcecaoHttpAsync(response, "Erro na Gemini API", cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var buffer = new StringBuilder();
        var dadosEvento = new StringBuilder();

        bool recebeuTexto = false;
        bool interacaoConcluida = false;

        async Task ProcessarBufferAsync(bool finalizar)
        {
            while (true)
            {
                string conteudoAtual = buffer.ToString();
                string? marcadorEncontrado = null;
                int indiceMarcador = -1;

                int indiceTurno = conteudoAtual.IndexOf(PausaTurno, StringComparison.Ordinal);
                int indiceDramatico = conteudoAtual.IndexOf(
                    PausaDramatica,
                    StringComparison.Ordinal
                );

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

                // Preserva o final do buffer para detectar marcadores divididos entre eventos SSE.
                string conteudo = buffer.ToString();
                int preservar = 0;
                int limite = Math.Min(
                    Math.Max(PausaTurno.Length, PausaDramatica.Length) - 1,
                    conteudo.Length
                );

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

        void ProcessarErroEvento(JsonElement raiz)
        {
            if (!raiz.TryGetProperty("error", out var erro))
                throw new HttpRequestException("O Gemini retornou um evento de erro sem detalhes.");

            string mensagem =
                erro.TryGetProperty("message", out var mensagemElemento)
                    ? mensagemElemento.GetString() ?? "Erro não especificado pelo Gemini."
                    : "Erro não especificado pelo Gemini.";

            string? codigo =
                erro.TryGetProperty("code", out var codigoElemento)
                    ? codigoElemento.ToString()
                    : null;

            HttpStatusCode? status = MapearCodigoErro(codigo);

            if (status.HasValue)
                throw new GeminiApiException($"Erro na Gemini API: {mensagem}", status.Value);

            throw new HttpRequestException($"Erro na Gemini API: {mensagem} (código: {codigo ?? "desconhecido"}).");
        }

        async Task ProcessarEventoAsync(string dados)
        {
            if (string.IsNullOrWhiteSpace(dados))
                return;

            dados = dados.Trim();

            if (
                dados.Equals("DONE", StringComparison.OrdinalIgnoreCase)
                || dados.Equals("[DONE]", StringComparison.OrdinalIgnoreCase)
            )
            {
                return;
            }

            if (!dados.StartsWith("{", StringComparison.Ordinal))
                return;

            using var documento = JsonDocument.Parse(dados);
            var raiz = documento.RootElement;

            string? tipoEvento =
                raiz.TryGetProperty("event_type", out var tipoElemento)
                    ? tipoElemento.GetString()
                    : null;

            if (string.Equals(tipoEvento, "error", StringComparison.Ordinal))
            {
                ProcessarErroEvento(raiz);
                return;
            }

            if (string.Equals(tipoEvento, "interaction.completed", StringComparison.Ordinal))
            {
                string? status = null;

                if (
                    raiz.TryGetProperty("interaction", out var interacao)
                    && interacao.TryGetProperty("status", out var statusElemento)
                )
                {
                    status = statusElemento.GetString();
                }

                if (!string.Equals(status, "completed", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"O Gemini encerrou a interação com status '{status ?? "desconhecido"}'."
                    );
                }

                interacaoConcluida = true;
                return;
            }

            // Compatível com step.delta da Interactions API e com respostas que tragam delta diretamente.
            if (
                tipoEvento != null
                && !string.Equals(tipoEvento, "step.delta", StringComparison.Ordinal)
            )
            {
                return;
            }

            if (!raiz.TryGetProperty("delta", out var delta))
                return;

            if (
                !delta.TryGetProperty("type", out var tipo)
                || tipo.GetString() != "text"
                || !delta.TryGetProperty("text", out var textoElemento)
            )
            {
                return;
            }

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

        if (dadosEvento.Length > 0)
            await ProcessarEventoAsync(dadosEvento.ToString());

        // Uma conexão encerrada sem interaction.completed pode ter sido truncada.
        if (!interacaoConcluida)
            throw new IOException("O fluxo do Gemini foi interrompido antes da conclusão da interação.");

        await ProcessarBufferAsync(true);

        if (!recebeuTexto)
            throw new InvalidOperationException("O Gemini não retornou uma narração válida.");
    }

    private static Dictionary<string, object?> CriarCorpoRequisicao(
        string modelo,
        string input,
        string systemInstruction,
        bool stream,
        int maxOutputTokens
    )
    {
        return new Dictionary<string, object?>
        {
            ["model"] = modelo,
            ["stream"] = stream,
            ["store"] = false,
            ["system_instruction"] = systemInstruction,
            ["input"] = input,
            ["generation_config"] = new { max_output_tokens = maxOutputTokens },
        };
    }

    private static HttpRequestMessage CriarRequest(
        Dictionary<string, object?> requestBody,
        string apiKey
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(requestBody);
        return request;
    }

    private static async Task<GeminiApiException> CriarExcecaoHttpAsync(
        HttpResponseMessage response,
        string contexto,
        CancellationToken cancellationToken
    )
    {
        string erro = await response.Content.ReadAsStringAsync(cancellationToken);
        string mensagem =
            $"{contexto}: {(int)response.StatusCode} {response.StatusCode}. Resposta: {erro}";

        return new GeminiApiException(mensagem, response.StatusCode, ObterRetryAfter(response));
    }

    private static TimeSpan? ObterRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan intervalo)
            return intervalo < TimeSpan.Zero ? TimeSpan.Zero : intervalo;

        if (retryAfter?.Date is DateTimeOffset data)
        {
            var restante = data - DateTimeOffset.UtcNow;
            return restante < TimeSpan.Zero ? TimeSpan.Zero : restante;
        }

        return null;
    }

    private static HttpStatusCode? MapearCodigoErro(string? codigo)
    {
        return (codigo ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "resource_exhausted" or "too_many_requests" => HttpStatusCode.TooManyRequests,
            "unavailable" => HttpStatusCode.ServiceUnavailable,
            "gateway_timeout" => HttpStatusCode.GatewayTimeout,
            "deadline_exceeded" => HttpStatusCode.RequestTimeout,
            "internal" or "internal_error" => HttpStatusCode.InternalServerError,
            "not_found" => HttpStatusCode.NotFound,
            "unauthenticated" => HttpStatusCode.Unauthorized,
            "permission_denied" => HttpStatusCode.Forbidden,
            "invalid_argument" or "invalid_request" or "bad_request" => HttpStatusCode.BadRequest,
            _ => null,
        };
    }

    private static bool EhModeloIndisponivel(HttpRequestException erro)
    {
        return erro.StatusCode == HttpStatusCode.NotFound;
    }

    private static bool EhErroTransitorio(Exception erro)
    {
        if (erro is HttpRequestException httpErro)
        {
            if (!httpErro.StatusCode.HasValue)
                return true;

            int status = (int)httpErro.StatusCode.Value;

            return status == 408 || status == 429 || status >= 500;
        }

        return erro is IOException or TimeoutException or TaskCanceledException;
    }

    private static TimeSpan CalcularEspera(int tentativa, Exception erro)
    {
        // Full Jitter: janela de 0–1s na primeira repetição e 0–2s na segunda.
        double limiteMs = Math.Min(1000 * Math.Pow(2, tentativa - 1), 2000);
        double esperaMs = Random.Shared.NextDouble() * limiteMs;

        if (erro is GeminiApiException apiErro && apiErro.RetryAfter is TimeSpan retryAfter)
            esperaMs = Math.Max(esperaMs, retryAfter.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(
            Math.Min(esperaMs, EsperaMaximaRetry.TotalMilliseconds)
        );
    }

    private async Task<T> ExecutarComFallbackAsync<T>(
        Func<string, CancellationToken, Task<T>> operacao,
        CancellationToken cancellationToken,
        Func<Task>? aoReiniciar = null
    )
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeoutOperacao);

        Exception? ultimoErro = null;
        int tentativasIniciadas = 0;

        foreach (string modelo in Modelos)
        {
            for (int tentativa = 1; tentativa <= MaxTentativasPorModelo; tentativa++)
            {
                try
                {
                    timeoutCts.Token.ThrowIfCancellationRequested();

                    // Se uma tentativa anterior já entregou texto, a interface deve descartar
                    // aquele trecho antes de receber a resposta completa da nova tentativa.
                    if (tentativasIniciadas > 0 && aoReiniciar != null)
                        await aoReiniciar();

                    tentativasIniciadas++;

                    Debug.WriteLine(
                        $"[PokéBus][Gemini] Modelo {modelo}: tentativa {tentativa}/{MaxTentativasPorModelo}."
                    );

                    return await operacao(modelo, timeoutCts.Token);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"A operação com o Gemini excedeu o limite de {TimeoutOperacao.TotalMinutes:0} minutos.",
                        ex
                    );
                }
                catch (HttpRequestException ex) when (EhModeloIndisponivel(ex))
                {
                    ultimoErro = ex;
                    Debug.WriteLine(
                        $"[PokéBus][Gemini] Modelo {modelo} indisponível (404). Avançando para o próximo modelo."
                    );
                    break;
                }
                catch (Exception ex) when (EhErroTransitorio(ex))
                {
                    ultimoErro = ex;

                    Debug.WriteLine(
                        $"[PokéBus][Gemini] Falha transitória em {modelo}, tentativa {tentativa}: {ex.Message}"
                    );

                    if (tentativa < MaxTentativasPorModelo)
                    {
                        TimeSpan espera = CalcularEspera(tentativa, ex);

                        try
                        {
                            await Task.Delay(espera, timeoutCts.Token);
                        }
                        catch (OperationCanceledException delayEx)
                            when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
                        {
                            throw new TimeoutException(
                                $"A operação com o Gemini excedeu o limite de {TimeoutOperacao.TotalMinutes:0} minutos.",
                                delayEx
                            );
                        }
                    }
                    else
                    {
                        Debug.WriteLine(
                            $"[PokéBus][Gemini] Limite de tentativas atingido para {modelo}; avançando para o próximo modelo."
                        );
                    }
                }
            }
        }

        if (ultimoErro != null)
            ExceptionDispatchInfo.Capture(ultimoErro).Throw();

        throw new InvalidOperationException("Não foi possível concluir a operação com nenhum modelo Gemini.");
    }

    private sealed class GeminiApiException : HttpRequestException
    {
        public TimeSpan? RetryAfter { get; }

        public GeminiApiException(
            string message,
            HttpStatusCode statusCode,
            TimeSpan? retryAfter = null
        )
            : base(message, null, statusCode)
        {
            RetryAfter = retryAfter;
        }
    }
}
