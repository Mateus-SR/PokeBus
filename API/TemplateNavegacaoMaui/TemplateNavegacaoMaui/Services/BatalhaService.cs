using System.Text;
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

public sealed class BatalhaService
{
    private const int MaximoTurnos = 6;

    public BatalhaPokemon SimularBatalha(Onibus onibusA, Onibus onibusB)
    {
        if (onibusA.Pokemon == null || onibusB.Pokemon == null)
            throw new ArgumentException("Os dois ônibus precisam ter Pokémon carregados.");

        if (onibusA == onibusB)
            throw new ArgumentException("Um ônibus não pode batalhar contra ele mesmo.");

        var participanteA = CriarParticipante(onibusA);
        var participanteB = CriarParticipante(onibusB);

        var batalha = new BatalhaPokemon
        {
            ParticipanteA = participanteA,
            ParticipanteB = participanteB,
        };

        // O Pokémon mais rápido começa.
        var atacante =
            participanteA.Pokemon.Speed >= participanteB.Pokemon.Speed
                ? participanteA
                : participanteB;

        var defensor = atacante == participanteA ? participanteB : participanteA;

        for (int turno = 1; turno <= MaximoTurnos; turno++)
        {
            if (atacante.EstaDerrotado || defensor.EstaDerrotado)
                break;

            ExecutarAtaque(batalha, atacante, defensor, turno);

            batalha.TotalTurnos = turno;

            if (defensor.EstaDerrotado)
                break;

            // Alterna os participantes.
            (atacante, defensor) = (defensor, atacante);
        }

        DeterminarVencedor(batalha);
        batalha.Relatorio = GerarRelatorio(batalha);

        return batalha;
    }

    private static ParticipanteBatalha CriarParticipante(Onibus onibus)
    {
        var pokemon = onibus.Pokemon!;

        // O HP de combate é ampliado para permitir confrontos mais longos.
        int hpMaximo = Math.Max(50, pokemon.Hp * 2 + 60);

        return new ParticipanteBatalha
        {
            Onibus = onibus,
            Pokemon = pokemon,
            HpMaximo = hpMaximo,
            HpAtual = hpMaximo,
        };
    }

    private static void ExecutarAtaque(
        BatalhaPokemon batalha,
        ParticipanteBatalha atacante,
        ParticipanteBatalha defensor,
        int turno
    )
    {
        var pokemonAtacante = atacante.Pokemon;
        var pokemonDefensor = defensor.Pokemon;

        // Escolhe entre ataque físico e especial.
        bool ataqueEspecial = pokemonAtacante.SpecialAttack >= pokemonAtacante.Attack;

        int poderAtaque = ataqueEspecial ? pokemonAtacante.SpecialAttack : pokemonAtacante.Attack;

        int defesa = ataqueEspecial ? pokemonDefensor.SpecialDefense : pokemonDefensor.Defense;

        string categoria = ataqueEspecial ? "Especial" : "Físico";

        // Usa um dos tipos do Pokémon atacante.
        string tipoAtaque =
            pokemonAtacante.Types.Count > 0
                ? pokemonAtacante.Types[Random.Shared.Next(pokemonAtacante.Types.Count)]
                : "normal";

        double efetividade = CalcularEfetividade(tipoAtaque, pokemonDefensor.Types);

        // Pequena variação aleatória no dano.
        double variacao = Random.Shared.Next(85, 101) / 100.0;

        double danoBase = (poderAtaque * 0.65 + 8) * (100.0 / (defesa + 100.0));

        int dano = Math.Max(1, (int)Math.Round(danoBase * efetividade * variacao));

        defensor.HpAtual = Math.Max(0, defensor.HpAtual - dano);

        string ataque = $"Ataque do tipo {FormatarTipo(tipoAtaque)}";

        batalha.Eventos.Add(
            new EventoBatalha
            {
                Turno = turno,
                Atacante = pokemonAtacante.Name,
                Defensor = pokemonDefensor.Name,
                Ataque = ataque,
                Dano = dano,
                Efetividade = efetividade,
                HpRestante = defensor.HpAtual,
                Categoria = categoria,
            }
        );
    }

    private static double CalcularEfetividade(string tipoAtaque, List<string> tiposDefensor)
    {
        double multiplicador = 1.0;

        foreach (var tipoDefensor in tiposDefensor)
        {
            multiplicador *= ObterMultiplicador(tipoAtaque, tipoDefensor);
        }

        return multiplicador;
    }

    private static double ObterMultiplicador(string ataque, string defesa)
    {
        // Imunidades.
        if ((ataque == "normal" || ataque == "fighting") && defesa == "ghost")
            return 0;

        if (ataque == "ghost" && defesa == "normal")
            return 0;

        if (ataque == "electric" && defesa == "ground")
            return 0;

        if (ataque == "ground" && defesa == "flying")
            return 0;

        if (ataque == "psychic" && defesa == "dark")
            return 0;

        if (ataque == "dragon" && defesa == "fairy")
            return 0;

        // Tipos contra os quais o ataque é super efetivo.
        bool superEfetivo = ataque switch
        {
            "normal" => false,
            "fire" => defesa is "grass" or "ice" or "bug" or "steel",
            "water" => defesa is "fire" or "ground" or "rock",
            "electric" => defesa is "water" or "flying",
            "grass" => defesa is "water" or "ground" or "rock",
            "ice" => defesa is "grass" or "ground" or "flying" or "dragon",
            "fighting" => defesa is "normal" or "ice" or "rock" or "dark" or "steel",
            "poison" => defesa is "grass" or "fairy",
            "ground" => defesa is "fire" or "electric" or "poison" or "rock" or "steel",
            "flying" => defesa is "grass" or "fighting" or "bug",
            "psychic" => defesa is "fighting" or "poison",
            "bug" => defesa is "grass" or "psychic" or "dark",
            "rock" => defesa is "fire" or "ice" or "flying" or "bug",
            "ghost" => defesa is "psychic" or "ghost",
            "dragon" => defesa is "dragon",
            "dark" => defesa is "psychic" or "ghost",
            "steel" => defesa is "ice" or "rock" or "fairy",
            "fairy" => defesa is "fighting" or "dragon" or "dark",
            _ => false,
        };

        if (superEfetivo)
            return 2.0;

        // Tipos contra os quais o ataque é pouco efetivo.
        bool poucoEfetivo = ataque switch
        {
            "normal" => defesa is "rock" or "steel",
            "fire" => defesa is "fire" or "water" or "rock" or "dragon",
            "water" => defesa is "water" or "grass" or "dragon",
            "electric" => defesa is "electric" or "grass" or "dragon",
            "grass" => defesa
                is "fire"
                    or "grass"
                    or "poison"
                    or "flying"
                    or "bug"
                    or "dragon"
                    or "steel",
            "ice" => defesa is "fire" or "water" or "ice" or "steel",
            "fighting" => defesa is "poison" or "flying" or "psychic" or "bug" or "fairy",
            "poison" => defesa is "poison" or "ground" or "rock" or "ghost",
            "ground" => defesa is "grass" or "bug",
            "flying" => defesa is "electric" or "rock" or "steel",
            "psychic" => defesa is "psychic" or "steel",
            "bug" => defesa
                is "fire"
                    or "fighting"
                    or "poison"
                    or "flying"
                    or "ghost"
                    or "steel"
                    or "fairy",
            "rock" => defesa is "fighting" or "ground" or "steel",
            "ghost" => defesa is "dark",
            "dragon" => defesa is "steel",
            "dark" => defesa is "fighting" or "dark" or "fairy",
            "steel" => defesa is "fire" or "water" or "electric" or "steel",
            "fairy" => defesa is "fire" or "poison" or "steel",
            _ => false,
        };

        return poucoEfetivo ? 0.5 : 1.0;
    }

    private static void DeterminarVencedor(BatalhaPokemon batalha)
    {
        var a = batalha.ParticipanteA;
        var b = batalha.ParticipanteB;

        if (a.EstaDerrotado && b.EstaDerrotado)
        {
            batalha.Empate = true;
            return;
        }

        if (a.EstaDerrotado)
        {
            batalha.Vencedor = b;
            return;
        }

        if (b.EstaDerrotado)
        {
            batalha.Vencedor = a;
            return;
        }

        // Se os dez turnos terminarem, compara a porcentagem de HP restante.
        double porcentagemA = (double)a.HpAtual / a.HpMaximo;
        double porcentagemB = (double)b.HpAtual / b.HpMaximo;

        if (porcentagemA > porcentagemB)
        {
            batalha.Vencedor = a;
        }
        else if (porcentagemB > porcentagemA)
        {
            batalha.Vencedor = b;
        }
        else
        {
            // Desempate pelos atributos de combate.
            int totalA = SomarAtributos(a.Pokemon);
            int totalB = SomarAtributos(b.Pokemon);

            if (totalA > totalB)
                batalha.Vencedor = a;
            else if (totalB > totalA)
                batalha.Vencedor = b;
            else
                batalha.Empate = true;
        }
    }

    private static int SomarAtributos(PokemonInfo pokemon)
    {
        return pokemon.Hp
            + pokemon.Attack
            + pokemon.Defense
            + pokemon.SpecialAttack
            + pokemon.SpecialDefense
            + pokemon.Speed;
    }

    private static string GerarRelatorio(BatalhaPokemon batalha)
    {
        var sb = new StringBuilder();

        var a = batalha.ParticipanteA;
        var b = batalha.ParticipanteB;

        sb.AppendLine("BATALHA POKÉBUS");
        sb.AppendLine();

        sb.AppendLine(
            $"{a.Pokemon.Name} (Linha {a.Onibus.CodigoLetreiro}, Sentido {a.Onibus.SentidoLinha})"
        );

        sb.AppendLine(
            $"{b.Pokemon.Name} (Linha {b.Onibus.CodigoLetreiro}, Sentido {b.Onibus.SentidoLinha})"
        );

        sb.AppendLine();
        sb.AppendLine($"Total de turnos: {batalha.TotalTurnos}");
        sb.AppendLine();

        foreach (var evento in batalha.Eventos)
        {
            sb.AppendLine($"Turno {evento.Turno}:");
            sb.AppendLine($"{evento.Atacante} usou {evento.Ataque}.");

            sb.AppendLine($"Categoria: {evento.Categoria}; dano: {evento.Dano}.");

            sb.AppendLine($"Efetividade: {evento.Efetividade}x.");

            sb.AppendLine($"HP restante de {evento.Defensor}: {evento.HpRestante}.");

            sb.AppendLine();
        }

        if (batalha.Empate)
        {
            sb.AppendLine("RESULTADO: EMPATE.");
        }
        else if (batalha.Vencedor != null)
        {
            sb.AppendLine($"VENCEDOR: {batalha.Vencedor.Pokemon.Name}.");
        }

        return sb.ToString();
    }

    private static string FormatarTipo(string tipo)
    {
        return tipo switch
        {
            "fire" => "Fogo",
            "water" => "Água",
            "electric" => "Elétrico",
            "grass" => "Planta",
            "ice" => "Gelo",
            "fighting" => "Lutador",
            "poison" => "Veneno",
            "ground" => "Terrestre",
            "flying" => "Voador",
            "psychic" => "Psíquico",
            "bug" => "Inseto",
            "rock" => "Pedra",
            "ghost" => "Fantasma",
            "dragon" => "Dragão",
            "dark" => "Sombrio",
            "steel" => "Aço",
            "fairy" => "Fada",
            _ => "Normal",
        };
    }
}
