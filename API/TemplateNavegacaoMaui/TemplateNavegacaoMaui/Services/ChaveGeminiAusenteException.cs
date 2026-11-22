namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Lançada quando uma chamada ao Gemini é feita sem que o usuário tenha informado sua chave.
/// As telas a tratam à parte para orientar o usuário a abrir as Configurações.
/// </summary>
public sealed class ChaveGeminiAusenteException : InvalidOperationException
{
    public ChaveGeminiAusenteException()
        : base("Nenhuma chave da Gemini API foi configurada. Abra \"Configurações\" no menu lateral e informe sua chave.")
    {
    }
}
