
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

public sealed class PokemonService
{
    private const string BaseUrl = "https://pokeapi.co/api/v2/pokemon/";

    private readonly HttpClient _httpClient;

    private readonly ConcurrentDictionary<int, PokemonInfo> _cache = new();

    public PokemonService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public PokemonService(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));
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

            var pokemon = await response.Content
                .ReadFromJsonAsync<PokemonApiResponse>();

            if (pokemon == null)
                return null;

            var info = ConverterResposta(pokemon);

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


    private static PokemonInfo ConverterResposta(PokemonApiResponse response)
    {
        var cultura = CultureInfo.InvariantCulture;

        var nome = cultura.TextInfo.ToTitleCase(
            response.Name.Replace('-', ' ')
        );

        var tipos = string.Join(" / ",
            response.Types.Select(x =>
                cultura.TextInfo.ToTitleCase(x.Type.Name)
            )
        );

        var habilidades = string.Join(", ",
            response.Abilities.Select(x =>
                cultura.TextInfo.ToTitleCase(
                    x.Ability.Name.Replace('-', ' ')
                ) + (x.IsHidden ? " (oculta)" : "")
            )
        );

        var estatisticas = string.Join(
            Environment.NewLine,
            response.Stats.Select(x =>
                $"{FormatarNome(x.Stat.Name)}: {x.BaseStat}"
            )
        );

        // Recupera os atributos individualmente.
        int ObterStat(string nomeStat)
        {
            return response.Stats.FirstOrDefault(
                x => x.Stat.Name == nomeStat
            )?.BaseStat ?? 0;
        }

        var sprite = response.Sprites.FrontDefault ??
                     response.Sprites.Other?.OfficialArtwork?.FrontDefault ??
                     string.Empty;

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

            Types = response.Types
                .Select(x => x.Type.Name)
                .ToList(),

            Hp = ObterStat("hp"),
            Attack = ObterStat("attack"),
            Defense = ObterStat("defense"),
            SpecialAttack = ObterStat("special-attack"),
            SpecialDefense = ObterStat("special-defense"),
            Speed = ObterStat("speed")
        };
    }


    private static string FormatarNome(string nome)
    {
        var cultura = CultureInfo.InvariantCulture;

        return cultura.TextInfo.ToTitleCase(
            nome.Replace('-', ' ')
        );
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
}
