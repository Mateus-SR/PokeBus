using Microsoft.Maui.Storage;

namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Guarda a chave no armazenamento seguro do sistema (<see cref="SecureStorage"/>).
/// Se ele não estiver disponível (por exemplo, em alguns apps Windows sem instalador),
/// usa <see cref="Preferences"/>, que não é criptografado, mas continua local ao dispositivo.
/// </summary>
public sealed class ConfiguracaoService : IConfiguracaoService
{
    private const string ChaveGemini = "gemini_api_key";

    public async Task<string?> ObterChaveGeminiAsync()
    {
        try
        {
            var chave = await SecureStorage.Default.GetAsync(ChaveGemini);

            if (!string.IsNullOrWhiteSpace(chave))
                return chave;
        }
        catch (Exception)
        {
            // Valor ilegível (ex.: restaurado de um backup do Android) ou armazenamento
            // seguro indisponível: descarta o valor e tenta o armazenamento comum.
            TentarRemoverDoArmazenamentoSeguro();
        }

        var alternativa = Preferences.Default.Get(ChaveGemini, string.Empty);

        return string.IsNullOrWhiteSpace(alternativa) ? null : alternativa;
    }

    public async Task SalvarChaveGeminiAsync(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
            throw new ArgumentException("A chave está vazia.", nameof(chave));

        chave = chave.Trim();

        try
        {
            await SecureStorage.Default.SetAsync(ChaveGemini, chave);

            // Evita deixar uma cópia sem criptografia de uma gravação anterior.
            Preferences.Default.Remove(ChaveGemini);
        }
        catch (Exception)
        {
            Preferences.Default.Set(ChaveGemini, chave);
        }
    }

    public void RemoverChaveGemini()
    {
        TentarRemoverDoArmazenamentoSeguro();

        Preferences.Default.Remove(ChaveGemini);
    }

    private static void TentarRemoverDoArmazenamentoSeguro()
    {
        try
        {
            SecureStorage.Default.Remove(ChaveGemini);
        }
        catch (Exception)
        {
            // Nada a fazer: se o armazenamento seguro não funciona, não há o que remover.
        }
    }
}
