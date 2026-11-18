
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Pages;

public partial class PokemonDetailsPage : ContentPage, IQueryAttributable
{
    public PokemonDetailsPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (query.TryGetValue(
            "OnibusSelecionado", out var resultado)
            && resultado is Onibus onibus)
        {
            BindingContext = onibus;
        }
    }
}
