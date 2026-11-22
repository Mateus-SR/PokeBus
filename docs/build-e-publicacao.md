# Build e publicação (GitHub Actions)

O workflow [`build.yml`](../.github/workflows/build.yml) gera os instaláveis do PokéBus:

| Arquivo | Plataforma | Observação |
| --- | --- | --- |
| `PokeBus-windows-x64.zip` | Windows 10/11 (64 bits) | Descompacte **a pasta inteira** e abra o `.exe`. O runtime já vem incluído. |
| `PokeBus-android.apk` | Android 5.0+ | Exige permitir a instalação de apps de fontes desconhecidas. |

iOS e Mac Catalyst não são gerados (exigem macOS e conta de desenvolvedor Apple).

## Quando ele roda

- **Pull request para a `main`** que altere `API/**` ou o próprio workflow: serve de verificação de que o projeto compila.
- **Manualmente**: aba *Actions* → *Build (Windows e Android)* → *Run workflow*.
- **Ao criar uma tag `v*`**: além de compilar, publica uma *Release* com os dois arquivos.

## Onde baixar

- Em qualquer execução: abra-a na aba *Actions* e baixe os *Artifacts* no fim da página.
- Em uma Release: aba *Releases* do repositório.

## Publicar uma versão

```bash
git tag v1.0.0
git push origin v1.0.0
```

A Release é criada automaticamente, com as notas geradas a partir dos commits e pull requests desde a versão anterior.

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
