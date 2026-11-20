namespace TemplateNavegacaoMaui.Models;

public sealed class GolpePokemon
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "normal";
    public string Categoria { get; set; } = string.Empty;
    public int Potencia { get; set; }
    public int Precisao { get; set; }
    public int PP { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
