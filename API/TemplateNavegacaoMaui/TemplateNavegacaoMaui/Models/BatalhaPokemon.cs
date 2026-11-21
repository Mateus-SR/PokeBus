
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Models;

public sealed class ParticipanteBatalha
{
    public ParticipanteBatalha(Onibus onibus, PokemonInfo pokemon, int hpMaximo)
    {
        Onibus = onibus ?? throw new ArgumentNullException(nameof(onibus));
        Pokemon = pokemon ?? throw new ArgumentNullException(nameof(pokemon));
        HpMaximo = hpMaximo;
        HpAtual = hpMaximo;
    }

    public Onibus Onibus { get; }

    public PokemonInfo Pokemon { get; }

    public int HpMaximo { get; }

    // Só o próprio participante altera o HP, por meio de ReceberDano.
    public int HpAtual { get; private set; }

    public bool EstaDerrotado => HpAtual <= 0;

    public void ReceberDano(int dano)
    {
        HpAtual = Math.Max(0, HpAtual - Math.Max(0, dano));
    }
}

public sealed class EventoBatalha
{
    public int Turno { get; set; }

    public string Atacante { get; set; } = string.Empty;

    public string Defensor { get; set; } = string.Empty;

    public string Ataque { get; set; } = string.Empty;

    public int Dano { get; set; }

    public double Efetividade { get; set; }

    public int HpRestante { get; set; }

    public string Categoria { get; set; } = string.Empty;
}

public sealed class BatalhaPokemon
{
    public required ParticipanteBatalha ParticipanteA { get; init; }

    public required ParticipanteBatalha ParticipanteB { get; init; }

    public List<EventoBatalha> Eventos { get; set; } = [];

    public ParticipanteBatalha? Vencedor { get; set; }

    public int TotalTurnos { get; set; }

    public bool Empate { get; set; }

    public string Relatorio { get; set; } = string.Empty;
}
