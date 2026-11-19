
using Microsoft.Extensions.Logging;
using TemplateNavegacaoMaui.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;

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
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular"
                );

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold"
                );

                fonts.AddFont(
                    "pokemon-emerald-pro.otf",
                    "PokemonEmerald"
                );
            });

        builder.Services.AddTransient<PokemonService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
