
using System.Collections.ObjectModel;
using TemplateNavegacaoMaui.Models;

namespace TemplateNavegacaoMaui.Services;

public static class OnibusStore
{
    public static ObservableCollection<Onibus> Onibus { get; } = [];
}
