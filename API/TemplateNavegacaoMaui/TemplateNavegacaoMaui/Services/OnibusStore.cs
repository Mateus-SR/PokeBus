using System.Collections.ObjectModel;
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

/// <summary>
/// Guarda os ônibus carregados do radar para que as telas compartilhem os mesmos dados.
/// Registrado como singleton no <c>MauiProgram</c>.
/// </summary>
public sealed class OnibusStore
{
    public ObservableCollection<Onibus> Onibus { get; } = [];
}
