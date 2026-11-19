
using TemplateNavegacaoMaui.Pages;

namespace TemplateNavegacaoMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(DetailsPage),
            typeof(DetailsPage));

        Routing.RegisterRoute(
            nameof(SecondaryDetailsPage),
            typeof(SecondaryDetailsPage));

        Routing.RegisterRoute(
            nameof(PokemonDetailsPage),
            typeof(PokemonDetailsPage));

        Routing.RegisterRoute(
            nameof(BatalhaPage),
            typeof(BatalhaPage));
    }
}
