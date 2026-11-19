
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui;

namespace TemplateNavegacaoMaui.Controls;

public class PokemonSpriteView : SKCanvasView
{
    private static readonly HttpClient _httpClient = new();

    private SKBitmap? _bitmap;
    private CancellationTokenSource? _loadCts;

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(
            nameof(Url),
            typeof(string),
            typeof(PokemonSpriteView),
            default(string),
            propertyChanged: OnUrlChanged
        );

    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public PokemonSpriteView()
    {
        PaintSurface += OnPaintSurface;
    }

    private static void OnUrlChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var view = (PokemonSpriteView)bindable;

        _ = view.CarregarImagemAsync(newValue as string);
    }

    private async Task CarregarImagemAsync(string? url)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();

        _loadCts = new CancellationTokenSource();

        var token = _loadCts.Token;

        if (string.IsNullOrWhiteSpace(url))
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _bitmap?.Dispose();
                _bitmap = null;
                InvalidateSurface();
            });

            return;
        }

        try
        {
            byte[] dados = await _httpClient.GetByteArrayAsync(
                url,
                token
            );

            token.ThrowIfCancellationRequested();

            using var stream = new MemoryStream(dados);

            var novoBitmap = SKBitmap.Decode(stream);

            if (novoBitmap == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (token.IsCancellationRequested)
                {
                    novoBitmap.Dispose();
                    return;
                }

                _bitmap?.Dispose();
                _bitmap = novoBitmap;

                InvalidateSurface();
            });
        }
        catch (OperationCanceledException)
        {
            // Uma nova URL substituiu o carregamento anterior.
        }
        catch (HttpRequestException)
        {
            await LimparImagemAsync(token);
        }
        catch (Exception)
        {
            await LimparImagemAsync(token);
        }
    }

    private async Task LimparImagemAsync(
        CancellationToken token)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (token.IsCancellationRequested)
                return;

            _bitmap?.Dispose();
            _bitmap = null;

            InvalidateSurface();
        });
    }

    private void OnPaintSurface(
    object? sender,
    SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        if (_bitmap == null ||
            _bitmap.Width <= 0 ||
            _bitmap.Height <= 0)
            return;

        float escala = Math.Min(
            (float)e.Info.Width / _bitmap.Width,
            (float)e.Info.Height / _bitmap.Height
        );

        float largura = _bitmap.Width * escala;
        float altura = _bitmap.Height * escala;

        float x = (e.Info.Width - largura) / 2;
        float y = (e.Info.Height - altura) / 2;

        var destino = new SKRect(
            x, y,
            x + largura,
            y + altura
        );

        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.None,
            IsAntialias = false
        };

        canvas.DrawBitmap(
            _bitmap,
            destino,
            paint
        );
    }
}
