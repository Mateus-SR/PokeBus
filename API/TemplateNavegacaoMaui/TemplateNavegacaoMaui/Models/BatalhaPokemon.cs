
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Models;

public sealed class ParticipanteBatalha
{
    public Onibus Onibus { get; set; } = null!;

    public PokemonInfo Pokemon { get; set; } = null!;

    public int HpMaximo { get; set; }

    public int HpAtual { get; set; }

    public bool EstaDerrotado => HpAtual <= 0;
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
    public ParticipanteBatalha ParticipanteA { get; set; } = null!;

    public ParticipanteBatalha ParticipanteB { get; set; } = null!;

    public List<EventoBatalha> Eventos { get; set; } = [];

    public ParticipanteBatalha? Vencedor { get; set; }

    public int TotalTurnos { get; set; }

    public bool Empate { get; set; }

    public string Relatorio { get; set; } = string.Empty;
}
