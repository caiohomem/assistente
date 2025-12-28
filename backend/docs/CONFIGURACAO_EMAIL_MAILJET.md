# Configuração de Email - Mailjet

O sistema está configurado para usar o Mailjet como provedor de email.

## Configuração Necessária

Adicione as seguintes configurações no `appsettings.json` ou `appsettings.Production.json`:

```json
{
  "Email": {
    "Mailjet": {
      "ApiKey": "75199ae1169bf79bf871b1f5383ec950",
      "SecretKey": "SUA_SECRET_KEY_AQUI",
      "From": "noreply@assistenteexecutivo.com",
      "FromName": "Assistente Executivo"
    }
  }
}
```

## Obtendo a Secret Key do Mailjet

1. Acesse o painel do Mailjet: https://app.mailjet.com/
2. Vá em **Account Settings** > **API Keys**
3. Você verá sua **API Key** e **Secret Key**
4. Copie a **Secret Key** e adicione no arquivo de configuração

## Configuração de Variáveis de Ambiente (Recomendado para Produção)

Para maior segurança, use variáveis de ambiente ao invés de colocar as chaves diretamente no arquivo:

```bash
Email__Mailjet__ApiKey=75199ae1169bf79bf871b1f5383ec950
Email__Mailjet__SecretKey=sua_secret_key_aqui
Email__Mailjet__From=noreply@assistenteexecutivo.com
Email__Mailjet__FromName=Assistente Executivo
```

## Verificação do Domínio

Certifique-se de que o domínio usado no campo `From` está verificado no Mailjet:
1. Acesse **Senders & Domains** no painel do Mailjet
2. Adicione e verifique o domínio `assistenteexecutivo.com`
3. Siga as instruções de verificação (DNS records)

## Teste

Após configurar, teste o envio de email criando um novo usuário. O sistema enviará automaticamente um email de boas-vindas usando o template configurado.

## Troubleshooting

- **Erro 401 (Unauthorized)**: Verifique se a API Key e Secret Key estão corretas
- **Erro 400 (Bad Request)**: Verifique se o domínio do remetente está verificado no Mailjet
- **Email não chega**: Verifique os logs da aplicação e o painel do Mailjet para ver o status do envio

