namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Guarda as configurações do usuário neste dispositivo (hoje, só a chave da Gemini API).
/// </summary>
public interface IConfiguracaoService
{
    /// <returns>A chave salva, ou <c>null</c> se não houver nenhuma.</returns>
    Task<string?> ObterChaveGeminiAsync();

    Task SalvarChaveGeminiAsync(string chave);

    void RemoverChaveGemini();
}
