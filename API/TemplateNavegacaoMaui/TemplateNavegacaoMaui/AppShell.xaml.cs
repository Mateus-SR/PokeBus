
using TemplateNavegacaoMaui.Pages;

namespace TemplateNavegacaoMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(PokemonDetailsPage),
            typeof(PokemonDetailsPage));

        Routing.RegisterRoute(
            nameof(BatalhaPage),
            typeof(BatalhaPage));
    }
}
