# Build e publicação (GitHub Actions)

O workflow [`build.yml`](../.github/workflows/build.yml) gera os instaláveis do PokéBus:

| Arquivo | Plataforma | Observação |
| --- | --- | --- |
| `PokeBus-windows-x64.zip` | Windows 10/11 (64 bits) | Descompacte **a pasta inteira** e abra o `.exe`. O runtime já vem incluído. |
| `PokeBus-android.apk` | Android 5.0+ (ARM 32 e 64 bits, e x64) | Exige permitir a instalação de apps de fontes desconhecidas. Inclui a arquitetura `armeabi-v7a` para celulares de 32 bits, o que deixa o arquivo maior. O workflow falha se ela estiver ausente. |

iOS e Mac Catalyst não são gerados (exigem macOS e conta de desenvolvedor Apple).

## Quando ele roda

- **Pull request para a `main`** que altere `API/**` ou o próprio workflow: serve de verificação de que o projeto compila.
- **Manualmente**: aba *Actions* → *Build (Windows e Android)* → *Run workflow*. Sem informar a versão, só gera os arquivos (como *Artifacts*); informando a versão, também publica uma *Release*.
- **Ao criar uma tag `v*`**: além de compilar, publica uma *Release* com os dois arquivos.

## Onde baixar

- Em qualquer execução: abra-a na aba *Actions* e baixe os *Artifacts* no fim da página.
- Em uma Release: aba *Releases* do repositório.

## Publicar uma versão

O nome da versão deve seguir o formato `v1.0.0` (começa com `v`). Há três jeitos, todos resultam na mesma Release com `PokeBus-windows-x64.zip` e `PokeBus-android.apk`:

**1. Pelo site, sem terminal (mais simples)**

1. Aba *Actions* → *Build (Windows e Android)* → botão *Run workflow*.
2. Escolha o branch `main`, preencha **Versão a publicar** (ex.: `v1.0.0`) e confirme.
3. Quando os jobs terminarem, a Release aparece na aba *Releases*, com as notas geradas a partir dos commits e pull requests desde a versão anterior.

**2. Por tag, no terminal**

```bash
git checkout main && git pull
git tag v1.0.0
git push origin v1.0.0
```

**3. Criando a Release pelo site**

*Releases* → *Draft a new release* → em *Choose a tag*, digite `v1.0.0` e escolha *Create new tag* → *Publish release*. O workflow roda e anexa os arquivos à Release que você criou (leva alguns minutos; a Release fica sem arquivos até o fim do build).

## Assinatura do APK

O APK precisa ser assinado para instalar. O workflow aceita duas situações:

- **Sem configurar nada:** gera uma chave temporária a cada execução. O APK instala, mas cada build tem uma assinatura diferente; para instalar uma versão nova, é preciso **desinstalar a anterior**. Um aviso aparece na execução.
- **Com uma chave permanente (recomendado se for distribuir):** assim, o Android aceita atualizar o app por cima.

### Criar a chave permanente (uma única vez)

```bash
keytool -genkeypair -keystore pokebus.keystore -alias pokebus \
  -keyalg RSA -keysize 2048 -validity 10000
```

Use a mesma senha para o keystore e para a chave (o workflow usa um único valor). **Guarde o arquivo e a senha em local seguro**: se forem perdidos, não será mais possível atualizar o app instalado.

Converta o arquivo para base64:

```bash
# Linux / macOS
base64 -w0 pokebus.keystore
```

```powershell
# Windows (PowerShell)
[Convert]::ToBase64String([IO.File]::ReadAllBytes("pokebus.keystore"))
```

Em *Settings → Secrets and variables → Actions* do repositório, crie:

| Secret | Valor |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | o texto em base64 gerado acima |
| `ANDROID_KEYSTORE_PASSWORD` | a senha do keystore/chave |
| `ANDROID_KEY_ALIAS` | o alias (opcional; o padrão é `pokebus`) |

Nunca faça commit do arquivo `.keystore`.

## Avisos

- O `.exe` não é assinado digitalmente, então o Windows pode exibir o aviso do SmartScreen ("Windows protegeu o computador"). Para abrir: *Mais informações → Executar assim mesmo*.
- A chave da Gemini **não** faz parte do build: cada pessoa a informa em *Configurações*, dentro do app.
