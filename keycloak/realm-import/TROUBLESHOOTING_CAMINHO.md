# Troubleshooting - Caminho do Arquivo JSON

## Problema: Arquivo JSON não encontrado

Se você receber o erro:
```
System.IO.FileNotFoundException: Arquivo JSON do realm não encontrado. Caminho configurado: keycloak/realm-import/assistenteexecutivo-realm.json
```

## Soluções

### Solução 1: Usar Caminho Absoluto (Recomendado)

No `appsettings.Development.json`, altere para usar caminho absoluto:

```json
{
  "Keycloak": {
    "UseJsonImport": true,
    "RealmJsonPath": "C:/Projects/AssistenteExecutivo/keycloak/realm-import/assistenteexecutivo-realm.json"
  }
}
```

**Substitua `C:/Projects/AssistenteExecutivo` pelo caminho real do seu projeto.**

### Solução 2: Verificar Diretório de Execução

O backend pode estar executando de um diretório diferente. Verifique:

1. **No Visual Studio**: O diretório de trabalho pode estar em `bin/Debug/net10.0/`
2. **No VS Code**: Verifique o `launch.json` para ver o `cwd` (current working directory)
3. **No Terminal**: Execute o backend a partir da raiz do projeto

### Solução 3: Copiar Arquivo para Diretório do Executável

Copie o arquivo JSON para o diretório onde o backend executa:

```powershell
# Se executando de bin/Debug/net10.0/
Copy-Item "keycloak\realm-import\assistenteexecutivo-realm.json" -Destination "bin\Debug\net10.0\keycloak\realm-import\"
```

### Solução 4: Usar Variável de Ambiente

Configure via variável de ambiente:

```powershell
$env:Keycloak__RealmJsonPath = "C:\Projects\AssistenteExecutivo\keycloak\realm-import\assistenteexecutivo-realm.json"
```

Ou no `launchSettings.json`:

```json
{
  "environmentVariables": {
    "Keycloak__RealmJsonPath": "C:/Projects/AssistenteExecutivo/keycloak/realm-import/assistenteexecutivo-realm.json"
  }
}
```

## Logs de Debug

O código agora mostra todos os caminhos tentados nos logs. Procure por:

```
[Error] Arquivo JSON não encontrado após tentar X caminhos diferentes:
  - caminho1 (existe: False)
  - caminho2 (existe: False)
  ...
```

Use esses logs para entender qual caminho o backend está tentando e ajuste o `RealmJsonPath` no appsettings.

## Verificação Rápida

Execute este comando PowerShell para encontrar o arquivo:

```powershell
Get-ChildItem -Path . -Filter "assistenteexecutivo-realm.json" -Recurse | Select-Object FullName
```

Use o caminho completo retornado no `appsettings.Development.json`.

