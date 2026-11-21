using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

public sealed class PokemonService : IPokemonService
{
    private const string BaseUrl = "https://pokeapi.co/api/v2/pokemon/";

    private readonly HttpClient _httpClient;

    // Os caches só funcionam entre telas porque o serviço é registrado como singleton no MauiProgram.
    private readonly ConcurrentDictionary<int, PokemonInfo> _cache = new();
    private readonly ConcurrentDictionary<string, GolpePokemon> _cacheGolpes = new();

    public PokemonService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<PokemonInfo?> ObterPokemonAsync(int id)
    {
        if (id <= 0)
            return null;

        if (_cache.TryGetValue(id, out var pokemonEmCache))
            return pokemonEmCache;

        try
        {
            var url = $"{BaseUrl}{id}";

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var pokemon = await response.Content.ReadFromJsonAsync<PokemonApiResponse>();

            if (pokemon == null)
                return null;

            var info = ConverterResposta(pokemon);
            info.Golpes = await ObterGolpesAsync(pokemon.Moves);

            // Diagnóstico dos golpes carregados.
            System.Diagnostics.Debug.WriteLine(
                $"[POKEMON] {info.Name} - {info.Golpes.Count} golpes carregados:"
            );

            foreach (var golpe in info.Golpes)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GOLPE] {golpe.Nome} | Tipo: {golpe.Tipo} | "
                        + $"Categoria: {golpe.Categoria} | Potência: {golpe.Potencia}"
                );
            }

            return _cache.GetOrAdd(id, info);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<List<GolpePokemon>> ObterGolpesAsync(List<PokemonMoveSlot> movimentos)
    {
        var golpesSelecionados = new List<GolpePokemon>();

        var candidatos = movimentos
            .Where(m => m.VersionGroupDetails.Any(v => v.MoveLearnMethod.Name == "level-up"))
            .Select(m => m.Move)
            .Where(m => !string.IsNullOrWhiteSpace(m.Url))
            .GroupBy(m => m.Name)
            .Select(g => g.First())
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        // Limite de consultas por Pokémon.
        foreach (var candidato in candidatos.Take(16))
        {
            if (golpesSelecionados.Count >= 4)
                break;

            try
            {
                var golpe = await ObterDetalhesGolpeAsync(candidato.Url);

                if (golpe == null)
                    continue;

                // Ignora golpes sem potência e golpes de status.
                if (golpe.Potencia <= 0 || golpe.Categoria == "status")
                    continue;

                golpesSelecionados.Add(golpe);
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erro ao consultar golpe {candidato.Name}: {ex.Message}"
                );
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erro ao interpretar golpe {candidato.Name}: {ex.Message}"
                );
            }
        }

        return golpesSelecionados;
    }

    private async Task<GolpePokemon?> ObterDetalhesGolpeAsync(string url)
    {
        if (_cacheGolpes.TryGetValue(url, out var golpeEmCache))
            return golpeEmCache;

        var resposta = await _httpClient.GetFromJsonAsync<MoveApiResponse>(url);

        if (resposta == null)
            return null;

        var descricao = resposta.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.Effect
            ?? "Golpe ofensivo.";

        var golpe = new GolpePokemon
        {
            Id = resposta.Id,
            Nome = resposta.Name,
            Tipo = resposta.Type.Name,
            Categoria = resposta.DamageClass.Name,
            Potencia = resposta.Power ?? 0,
            Precisao = resposta.Accuracy ?? 100,
            PP = resposta.Pp ?? 0,
            Descricao = descricao,
        };

        return _cacheGolpes.GetOrAdd(url, golpe);
    }

    private static PokemonInfo ConverterResposta(PokemonApiResponse response)
    {
        var cultura = CultureInfo.InvariantCulture;

        var nome = cultura.TextInfo.ToTitleCase(response.Name.Replace('-', ' '));

        var tipos = string.Join(
            " / ",
            response.Types.Select(x => cultura.TextInfo.ToTitleCase(x.Type.Name))
        );

        var habilidades = string.Join(
            ", ",
            response.Abilities.Select(x =>
                cultura.TextInfo.ToTitleCase(x.Ability.Name.Replace('-', ' '))
                + (x.IsHidden ? " (oculta)" : "")
            )
        );

        var estatisticas = string.Join(
            Environment.NewLine,
            response.Stats.Select(x => $"{FormatarNome(x.Stat.Name)}: {x.BaseStat}")
        );

        // Recupera os atributos individualmente.
        int ObterStat(string nomeStat)
        {
            return response.Stats.FirstOrDefault(x => x.Stat.Name == nomeStat)?.BaseStat ?? 0;
        }

        var sprite = response.Sprites.FrontDefault
            ?? response.Sprites.Other?.OfficialArtwork?.FrontDefault
            ?? string.Empty;

        return new PokemonInfo
        {
            Id = response.Id,
            Name = nome,
            SpriteUrl = sprite,

            HeightText = $"{(response.Height / 10m).ToString("0.0", cultura)} m",
            WeightText = $"{(response.Weight / 10m).ToString("0.0", cultura)} kg",

            TypesText = tipos,
            AbilitiesText = habilidades,
            BaseExperienceText = response.BaseExperience?.ToString() ?? "Não informado",
            StatsText = estatisticas,

            Types = response.Types.Select(x => x.Type.Name).ToList(),

            Hp = ObterStat("hp"),
            Attack = ObterStat("attack"),
            Defense = ObterStat("defense"),
            SpecialAttack = ObterStat("special-attack"),
            SpecialDefense = ObterStat("special-defense"),
            Speed = ObterStat("speed"),
        };
    }

    private static string FormatarNome(string nome)
    {
        var cultura = CultureInfo.InvariantCulture;

        return cultura.TextInfo.ToTitleCase(nome.Replace('-', ' '));
    }

    // Classes de mapeamento da API

    public sealed class PokemonApiResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("base_experience")]
        public int? BaseExperience { get; set; }

        [JsonPropertyName("types")]
        public List<PokemonTypeSlot> Types { get; set; } = [];

        [JsonPropertyName("abilities")]
        public List<PokemonAbilitySlot> Abilities { get; set; } = [];

        [JsonPropertyName("stats")]
        public List<PokemonStatSlot> Stats { get; set; } = [];

        [JsonPropertyName("sprites")]
        public PokemonSprites Sprites { get; set; } = new();

        [JsonPropertyName("moves")]
        public List<PokemonMoveSlot> Moves { get; set; } = [];
    }

    public sealed class PokemonTypeSlot
    {
        [JsonPropertyName("type")]
        public PokemonResource Type { get; set; } = new();
    }

    public sealed class PokemonAbilitySlot
    {
        [JsonPropertyName("ability")]
        public PokemonResource Ability { get; set; } = new();

        [JsonPropertyName("is_hidden")]
        public bool IsHidden { get; set; }
    }

    public sealed class PokemonStatSlot
    {
        [JsonPropertyName("base_stat")]
        public int BaseStat { get; set; }

        [JsonPropertyName("stat")]
        public PokemonResource Stat { get; set; } = new();
    }

    public sealed class PokemonResource
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
    }

    public sealed class PokemonSprites
    {
        [JsonPropertyName("front_default")]
        public string? FrontDefault { get; set; }

        [JsonPropertyName("other")]
        public PokemonOtherSprites? Other { get; set; }
    }

    public sealed class PokemonOtherSprites
    {
        [JsonPropertyName("official-artwork")]
        public PokemonOfficialArtwork? OfficialArtwork { get; set; }
    }

    public sealed class PokemonOfficialArtwork
    {
        [JsonPropertyName("front_default")]
        public string? FrontDefault { get; set; }
    }

    // Mapeamento dos golpes disponíveis do Pokémon.

    public sealed class PokemonMoveSlot
    {
        [JsonPropertyName("move")]
        public PokemonResource Move { get; set; } = new();

        [JsonPropertyName("version_group_details")]
        public List<PokemonMoveVersionDetail> VersionGroupDetails { get; set; } = [];
    }

    public sealed class PokemonMoveVersionDetail
    {
        [JsonPropertyName("move_learn_method")]
        public PokemonResource MoveLearnMethod { get; set; } = new();
    }

    // Mapeamento dos detalhes individuais de cada golpe.

    private sealed class MoveApiResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("power")]
        public int? Power { get; set; }

        [JsonPropertyName("accuracy")]
        public int? Accuracy { get; set; }

        [JsonPropertyName("pp")]
        public int? Pp { get; set; }

        [JsonPropertyName("type")]
        public PokemonResource Type { get; set; } = new();

        [JsonPropertyName("damage_class")]
        public PokemonResource DamageClass { get; set; } = new();

        [JsonPropertyName("effect_entries")]
        public List<MoveEffectEntry> EffectEntries { get; set; } = [];
    }

    private sealed class MoveEffectEntry
    {
        [JsonPropertyName("effect")]
        public string Effect { get; set; } = "";

        [JsonPropertyName("language")]
        public PokemonResource Language { get; set; } = new();
    }
}
