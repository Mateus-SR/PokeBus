namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Gera a narração de uma batalha com IA generativa, entregando o texto aos poucos (streaming).
/// </summary>
public interface IGeminiService
{
    Task NarrarBatalhaEmStreamingAsync(
        string relatorio,
        Func<string, Task> aoReceberTexto,
        CancellationToken cancellationToken = default
    );
}
