using AssistenteExecutivo.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedEmailTemplatesAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        // Verificar se já existem templates
        if (await context.EmailTemplates.AnyAsync(cancellationToken))
        {
            return; // Já foram seedados
        }

        var templates = new List<EmailTemplate>
        {
            new EmailTemplate(
                name: "Bem-vindo ao Assistente Executivo",
                templateType: EmailTemplateType.UserCreated,
                subject: "Bem-vindo ao Assistente Executivo, {{ NomeUsuario }}!",
                htmlBody: @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #4CAF50; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bem-vindo ao Assistente Executivo!</h1>
        </div>
        <div class='content'>
            <p>Olá, {{ NomeUsuario }}!</p>
            <p>Sua conta foi criada com sucesso. Agora você pode começar a usar todas as funcionalidades do Assistente Executivo.</p>
            <p>Se você tiver alguma dúvida, não hesite em entrar em contato conosco.</p>
            <p>Bem-vindo!</p>
        </div>
        <div class='footer'>
            <p>Assistente Executivo - Seu assistente inteligente</p>
        </div>
    </div>
</body>
</html>"),

            new EmailTemplate(
                name: "Recuperação de Senha",
                templateType: EmailTemplateType.PasswordReset,
                subject: "Recuperação de Senha - Assistente Executivo",
                htmlBody: @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #2196F3; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background-color: #f9f9f9; }
        .button { display: inline-block; padding: 12px 24px; background-color: #2196F3; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }
        .warning { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 20px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Recuperação de Senha</h1>
        </div>
        <div class='content'>
            <p>Olá, {{ NomeUsuario }}!</p>
            <p>Recebemos uma solicitação para redefinir sua senha. Clique no botão abaixo para criar uma nova senha:</p>
            <p style='text-align: center;'>
                <a href='{{ ResetSenhaUrl }}' class='button'>Redefinir Senha</a>
            </p>
            <p>Ou copie e cole o link abaixo no seu navegador:</p>
            <p style='word-break: break-all;'><a href='{{ ResetSenhaUrl }}'>{{ ResetSenhaUrl }}</a></p>
            <div class='warning'>
                <p><strong>Importante:</strong> Este link expira em 24 horas. Se você não solicitou esta recuperação de senha, ignore este email.</p>
            </div>
        </div>
        <div class='footer'>
            <p>Assistente Executivo - Seu assistente inteligente</p>
        </div>
    </div>
</body>
</html>")
        };

        await context.EmailTemplates.AddRangeAsync(templates, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}

