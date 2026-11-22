# PokeBus

Versão paródia do [Busca Bus](https://github.com/Mateus-SR/Busca-Bus-React) com Pokémon, feita em **.NET MAUI** como projeto da faculdade (prova de conceito).

Cada ônibus em circulação vira um Pokémon: os **três últimos dígitos do ID do veículo** definem o número na Pokédex (1 a 999). Na aba Arena, dois ônibus sorteados batalham e uma IA narra a luta.

## O que o app usa

| Recurso | Para quê |
| --- | --- |
| **Olho Vivo (SPTrans)**, via backend do Busca Bus | Ônibus em circulação, previsões de chegada |
| **PokeAPI** | Dados, sprites e golpes dos Pokémon |
| **Gemini API** | Narração da batalha em streaming e tradução das descrições |

Navegação: **Flyout** (Área principal / Sobre o projeto / Configurações), **Tabs** (Monitoramento / Arena) e **pilha** (detalhes do Pokémon e tela da batalha).

## Como rodar

Requisitos: .NET 10 com o workload MAUI (`dotnet workload install maui`) e Visual Studio 2022+ (ou outra IDE com suporte a MAUI).

1. Abra `API/TemplateNavegacaoMaui/TemplateNavegacaoMaui` e execute o projeto no alvo desejado (Windows, Android...).
2. Para a narração das batalhas e a tradução da Pokédex, informe uma chave da Gemini API em **Configurações** (menu lateral). Você pode gerar uma gratuitamente no [Google AI Studio](https://aistudio.google.com/apikey).

### Sobre a chave da Gemini

- Cada pessoa usa a **própria chave**: ela é digitada no app e fica guardada somente no dispositivo (no armazenamento seguro do sistema, quando disponível) e é enviada apenas ao Google.
- Sem chave, o app funciona normalmente; só a narração e a tradução ficam desativadas, e as telas avisam como configurar.
- Isso vale para qualquer alvo (Windows, Android...), sem depender de variável de ambiente.

## Organização do código

```
Models/     dados da aplicação (Onibus, PokemonInfo, BatalhaPokemon...)
Services/   acesso às APIs e regras da batalha, cada um atrás de uma interface
Pages/      telas (XAML + code-behind)
Controls/   PokemonSpriteView, controle SkiaSharp que desenha o sprite
```

As dependências (`IRadarService`, `IPokemonService`, `IGeminiService`, `IBatalhaService`, `IConfiguracaoService` e o `OnibusStore`) são registradas no `MauiProgram` e recebidas pelas páginas no construtor.
