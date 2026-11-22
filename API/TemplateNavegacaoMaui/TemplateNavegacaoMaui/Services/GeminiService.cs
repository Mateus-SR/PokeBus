using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TemplateNavegacaoMaui.Services;

public sealed class GeminiService : IGeminiService
{
    private const string ApiUrl =
        "https://generativelanguage.googleapis.com/v1beta/interactions?alt=sse";

    private const string ApiUrlSemStreaming =
        "https://generativelanguage.googleapis.com/v1beta/interactions";

    private static readonly ConcurrentDictionary<string, string> CacheTraducoes = new();

    private const string Model = "gemini-3.1-flash-lite";

    private const string PausaTurno = "[[PAUSA_TURNO]]";
    private const string PausaDramatica = "[[PAUSA_DRAMATICA]]";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
    }

    public async Task<string> TraduzirDescricaoPokedexAsync(
        string descricao,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição está vazia.", nameof(descricao));

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "A variável de ambiente GEMINI_API_KEY não foi configurada."
            );
        }

        // Reaproveita traduções já realizadas.
        if (CacheTraducoes.TryGetValue(descricao, out var traducaoEmCache))
            return traducaoEmCache;

        var requestBody = new
        {
            model = Model,
            stream = false,
            store = false,

            system_instruction = """
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
                """,

            input = descricao,

            generation_config = new { max_output_tokens = 500 },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrlSemStreaming);

        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Erro na tradução Gemini: {(int)response.StatusCode} "
                    + $"{response.StatusCode}. Resposta: {erro}"
            );
        }

        var conteudo = await response.Content.ReadAsStringAsync(cancellationToken);

        using var documento = JsonDocument.Parse(conteudo);

        var raiz = documento.RootElement;

        if (!raiz.TryGetProperty("status", out var status) || status.GetString() != "completed")
        {
            throw new InvalidOperationException("O Gemini não concluiu a tradução.");
        }

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
        {
            throw new InvalidOperationException("O Gemini retornou uma tradução vazia.");
        }

        // Guarda a tradução para reutilização durante a execução do aplicativo.
        CacheTraducoes.TryAdd(descricao, traducao);

        return CacheTraducoes[descricao];
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
                  {{PausaTurno}} para turnos normais.
                  {{PausaDramatica}} para turnos com momentos decisivos, como uma derrota ou o golpe final.
                - Não insira marcadores de pausa durante o diálogo de apresentação, antes do primeiro turno.
                - Não insira marcadores depois da declaração final do vencedor.
                - Os marcadores devem ficar entre os turnos, nunca no meio de uma ação.
                - Não escreva os marcadores de pausa em nenhum outro lugar.

                FINALIZAÇÃO:

                Termine com uma declaração emocionante do vencedor, mencionando a linha do ônibus e o nome inteiro dele.

                Antes de concluir, verifique se todos os marcadores de formatação estão corretamente abertos e fechados.
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
