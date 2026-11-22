# PokeBus

Versão paródia do [Busca Bus](https://github.com/Mateus-SR/Busca-Bus-React) com Pokémon, feita em **.NET MAUI** como projeto da faculdade (prova de conceito).

Cada ônibus em circulação vira um Pokémon: os **três últimos dígitos do ID do veículo** definem o número na Pokédex (1 a 999). Na aba Arena, dois ônibus sorteados batalham e uma IA narra a luta.

## O que o app usa

| Recurso | Para quê |
| --- | --- |
| **Olho Vivo (SPTrans)**, via backend do Busca Bus | Ônibus em circulação |
| **PokeAPI** | Dados, sprites e golpes dos Pokémon |
| **Gemini API** | Narração da batalha em streaming |

Navegação: **Flyout** (Área principal / Sobre o projeto), **Tabs** (Monitoramento / Arena) e **pilha** (detalhes do Pokémon e tela da batalha).

## Como rodar

Requisitos: .NET 10 com o workload MAUI (`dotnet workload install maui`) e Visual Studio 2022+ (ou outra IDE com suporte a MAUI).

1. Abra `API/TemplateNavegacaoMaui/TemplateNavegacaoMaui` e execute o projeto no alvo desejado (Windows, Android...).
2. Para a narração funcionar, defina a variável de ambiente `GEMINI_API_KEY` com uma chave da Gemini API antes de abrir a IDE.

> **Limitação conhecida:** a chave é lida de uma variável de ambiente, então a narração só funciona onde essa variável existe (ex.: Windows em desenvolvimento), não em um celular. Por ser uma prova de conceito, a chave não passa por um backend.

## Organização do código

```
Models/     dados da aplicação (Onibus, PokemonInfo, BatalhaPokemon...)
Services/   acesso às APIs e regras da batalha, cada um atrás de uma interface
Pages/      telas (XAML + code-behind)
Controls/   PokemonSpriteView, controle SkiaSharp que desenha o sprite
```

As dependências (`IRadarService`, `IPokemonService`, `IGeminiService`, `IBatalhaService` e o `OnibusStore`) são registradas no `MauiProgram` e recebidas pelas páginas no construtor.
