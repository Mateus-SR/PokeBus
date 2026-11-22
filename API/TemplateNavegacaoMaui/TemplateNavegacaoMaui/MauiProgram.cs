using DotNet.Meteor.HotReload.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TemplateNavegacaoMaui.Pages;
using TemplateNavegacaoMaui.Services;

namespace TemplateNavegacaoMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("pokemon-emerald-pro.otf", "PokemonEmerald");
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.EnableHotReload();
#endif

        RegistrarServicos(builder.Services);
        RegistrarPaginas(builder.Services);

        return builder.Build();
    }

    // Serviços são singletons: cada um recebe seu HttpClient
    // e o PokemonService mantém seu cache entre as telas.
    private static void RegistrarServicos(IServiceCollection services)
    {
        services.AddSingleton<OnibusStore>();

        services.AddSingleton<IRadarService>(_ => new RadarService(new HttpClient()));

        services.AddSingleton<IPokemonService>(_ => new PokemonService(
            new HttpClient { Timeout = TimeSpan.FromSeconds(30) }
        ));

        services.AddSingleton<IConfiguracaoService, ConfiguracaoService>();

        // A narração em streaming pode demorar, por isso o timeout é maior.
        services.AddSingleton<IGeminiService>(sp => new GeminiService(
            new HttpClient { Timeout = TimeSpan.FromSeconds(90) },
            sp.GetRequiredService<IConfiguracaoService>()
        ));

        services.AddSingleton<IBatalhaService>(_ => new BatalhaService(Random.Shared));
    }

    // Páginas registradas no container para receberem suas dependências pelo construtor.
    private static void RegistrarPaginas(IServiceCollection services)
    {
        services.AddTransient<MainPage>();
        services.AddTransient<ContentPageTemplate>();
        services.AddTransient<AboutPage>();
        services.AddTransient<PokemonDetailsPage>();
        services.AddTransient<BatalhaPage>();
        services.AddTransient<AboutIAPage>();
        services.AddTransient<ConfiguracoesPage>();
    }
}
