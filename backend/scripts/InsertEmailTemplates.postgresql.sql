-- Script para inserir templates de email do sistema
-- Este script cria templates profissionais de email para o Assistente Executivo

DO $$
BEGIN
    DELETE FROM "EmailTemplates"
    WHERE "TemplateType" IN (1, 2, 3, 4, 5, 6);

    -- Template de Boas-vindas (UserCreated)
    INSERT INTO "EmailTemplates" ("Id", "Name", "TemplateType", "Subject", "HtmlBody", "IsActive", "CreatedAt")
    VALUES (
        gen_random_uuid(),
        'Bem-vindo ao Assistente Executivo',
        1, -- UserCreated
        'Bem-vindo ao Assistente Executivo, {{ NomeUsuario }}!',
        '<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bem-vindo ao Assistente Executivo</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #18181b;
            background-color: #fafafa;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
            color: #ffffff;
            padding: 40px 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 28px;
            font-weight: 700;
            margin-bottom: 10px;
        }
        .header p {
            font-size: 16px;
            opacity: 0.95;
        }
        .content {
            padding: 40px 30px;
            background-color: #ffffff;
        }
        .content h2 {
            color: #18181b;
            font-size: 22px;
            margin-bottom: 20px;
            font-weight: 600;
        }
        .content p {
            color: #52525b;
            font-size: 16px;
            margin-bottom: 16px;
        }
        .highlight-box {
            background-color: #f4f4f5;
            border-left: 4px solid #6366f1;
            padding: 20px;
            margin: 24px 0;
            border-radius: 4px;
        }
        .button {
            display: inline-block;
            background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 14px 32px;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            margin: 24px 0;
            text-align: center;
            transition: all 0.3s ease;
        }
        .button:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
        }
        .features {
            margin: 30px 0;
        }
        .feature-item {
            display: flex;
            align-items: start;
            margin-bottom: 16px;
            color: #52525b;
        }
        .feature-icon {
            color: #6366f1;
            margin-right: 12px;
            font-size: 20px;
            flex-shrink: 0;
        }
        .footer {
            background-color: #fafafa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e4e4e7;
        }
        .footer p {
            color: #71717a;
            font-size: 14px;
            margin-bottom: 8px;
        }
        .footer a {
            color: #6366f1;
            text-decoration: none;
        }
        .social-links {
            margin-top: 20px;
        }
        .social-links a {
            display: inline-block;
            margin: 0 8px;
            color: #71717a;
            text-decoration: none;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>🎉 Bem-vindo ao Assistente Executivo!</h1>
            <p>Sua jornada de produtividade começa agora</p>
        </div>
        
        <div class="content">
            <h2>Olá, {{ NomeUsuario }}!</h2>
            
            <p>Sua conta foi criada com sucesso. Estamos muito felizes em tê-lo(a) conosco!</p>
            
            <div class="highlight-box">
                <p><strong>O Assistente Executivo</strong> é sua ferramenta completa para gerenciar contatos, notas, lembretes e muito mais, tudo com o poder da inteligência artificial.</p>
            </div>
            
            <p>Para começar, você pode:</p>
            
            <div class="features">
                <div class="feature-item">
                    <span class="feature-icon">📇</span>
                    <span><strong>Gerenciar seus contatos</strong> - Organize e mantenha todas as suas informações de contato em um só lugar</span>
                </div>
                <div class="feature-item">
                    <span class="feature-icon">📝</span>
                    <span><strong>Criar notas inteligentes</strong> - Capture informações importantes com notas de texto ou áudio</span>
                </div>
                <div class="feature-item">
                    <span class="feature-icon">⏰</span>
                    <span><strong>Definir lembretes</strong> - Nunca perca um follow-up importante</span>
                </div>
                <div class="feature-item">
                    <span class="feature-icon">🤖</span>
                    <span><strong>Usar IA para automação</strong> - Deixe a inteligência artificial trabalhar para você</span>
                </div>
            </div>
            
            <div style="text-align: center;">
                <a href="{{ AppUrl }}" class="button">Acessar Minha Conta</a>
            </div>
            
            <p style="margin-top: 30px; color: #71717a; font-size: 14px;">
                Se você tiver alguma dúvida ou precisar de ajuda, nossa equipe de suporte está sempre pronta para ajudar.
            </p>
        </div>
        
        <div class="footer">
            <p><strong>Assistente Executivo</strong></p>
            <p>Seu assistente inteligente para produtividade executiva</p>
            <p style="margin-top: 16px;">
                <a href="{{ AppUrl }}">Acessar o sistema</a> | 
                <a href="{{ SupportUrl }}">Suporte</a> | 
                <a href="{{ PrivacyUrl }}">Privacidade</a>
            </p>
            <div class="social-links">
                <p style="font-size: 12px; color: #a1a1aa;">
                    © 2024 Assistente Executivo. Todos os direitos reservados.
                </p>
            </div>
        </div>
    </div>
</body>
</html>',
        true,
        NOW()
    );

    -- Template de Recuperação de Senha (PasswordReset)
    INSERT INTO "EmailTemplates" ("Id", "Name", "TemplateType", "Subject", "HtmlBody", "IsActive", "CreatedAt")
    VALUES (
        gen_random_uuid(),
        'Recuperação de Senha',
        2, -- PasswordReset
        'Recuperação de Senha - Assistente Executivo',
        '<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Recuperação de Senha</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #18181b;
            background-color: #fafafa;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
            color: #ffffff;
            padding: 40px 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 28px;
            font-weight: 700;
            margin-bottom: 10px;
        }
        .content {
            padding: 40px 30px;
            background-color: #ffffff;
        }
        .content h2 {
            color: #18181b;
            font-size: 22px;
            margin-bottom: 20px;
            font-weight: 600;
        }
        .content p {
            color: #52525b;
            font-size: 16px;
            margin-bottom: 16px;
        }
        .button {
            display: inline-block;
            background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 14px 32px;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            margin: 24px 0;
            text-align: center;
        }
        .warning-box {
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 20px;
            margin: 24px 0;
            border-radius: 4px;
        }
        .warning-box p {
            color: #92400e;
            margin: 0;
        }
        .link-box {
            background-color: #f4f4f5;
            padding: 16px;
            margin: 20px 0;
            border-radius: 4px;
            word-break: break-all;
            font-size: 14px;
            color: #52525b;
        }
        .footer {
            background-color: #fafafa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e4e4e7;
        }
        .footer p {
            color: #71717a;
            font-size: 14px;
            margin-bottom: 8px;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>🔐 Recuperação de Senha</h1>
        </div>
        
        <div class="content">
            <h2>Olá, {{ NomeUsuario }}!</h2>
            
            <p>Recebemos uma solicitação para redefinir sua senha no Assistente Executivo.</p>
            
            <p>Clique no botão abaixo para criar uma nova senha:</p>
            
            <div style="text-align: center;">
                <a href="{{ ResetSenhaUrl }}" class="button">Redefinir Minha Senha</a>
            </div>
            
            <p>Ou copie e cole o link abaixo no seu navegador:</p>
            
            <div class="link-box">
                {{ ResetSenhaUrl }}
            </div>
            
            <div class="warning-box">
                <p><strong>⚠️ Importante:</strong> Este link expira em 24 horas. Se você não solicitou esta recuperação de senha, ignore este email. Sua senha permanecerá inalterada.</p>
            </div>
            
            <p style="color: #71717a; font-size: 14px; margin-top: 30px;">
                Por segurança, nunca compartilhe este link com outras pessoas.
            </p>
        </div>
        
        <div class="footer">
            <p><strong>Assistente Executivo</strong></p>
            <p>Se você não solicitou esta recuperação, pode ignorar este email com segurança.</p>
        </div>
    </div>
</body>
</html>',
        true,
        NOW()
    );

    -- Template de Boas-vindas Genérico (Welcome)
    INSERT INTO "EmailTemplates" ("Id", "Name", "TemplateType", "Subject", "HtmlBody", "IsActive", "CreatedAt")
    VALUES (
        gen_random_uuid(),
        'Mensagem de Boas-vindas',
        3, -- Welcome
        'Bem-vindo ao Assistente Executivo!',
        '<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Bem-vindo</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #18181b;
            background-color: #fafafa;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
            color: #ffffff;
            padding: 40px 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 28px;
            font-weight: 700;
        }
        .content {
            padding: 40px 30px;
            background-color: #ffffff;
        }
        .content p {
            color: #52525b;
            font-size: 16px;
            margin-bottom: 16px;
        }
        .footer {
            background-color: #fafafa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e4e4e7;
        }
        .footer p {
            color: #71717a;
            font-size: 14px;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>Bem-vindo ao Assistente Executivo!</h1>
        </div>
        
        <div class="content">
            <p>Olá, {{ NomeUsuario }}!</p>
            
            <p>{{ Mensagem }}</p>
            
            <p>Estamos felizes em tê-lo(a) conosco!</p>
        </div>
        
        <div class="footer">
            <p><strong>Assistente Executivo</strong></p>
        </div>
    </div>
</body>
</html>',
        true,
        NOW()
    );

    -- Template de Proposta de Acordo (AgreementProposal)
    INSERT INTO "EmailTemplates" ("Id", "Name", "TemplateType", "Subject", "HtmlBody", "IsActive", "CreatedAt")
    VALUES (
        gen_random_uuid(),
        'Acordo - Proposta de Aceite',
        4, -- AgreementProposal
        'Aceite do acordo: {{ AgreementTitle }}',
        '<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Proposta de Acordo</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #18181b;
            background-color: #f8fafc;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #0ea5e9 0%, #2563eb 100%);
            color: #ffffff;
            padding: 36px 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 26px;
            font-weight: 700;
        }
        .content {
            padding: 36px 30px;
            background-color: #ffffff;
        }
        .content h2 {
            color: #18181b;
            font-size: 20px;
            margin-bottom: 16px;
            font-weight: 600;
        }
        .content p {
            color: #52525b;
            font-size: 16px;
            margin-bottom: 12px;
        }
        .details {
            background-color: #f4f4f5;
            padding: 16px;
            border-radius: 6px;
            margin: 20px 0;
        }
        .details p {
            margin-bottom: 8px;
        }
        .button {
            display: inline-block;
            background: linear-gradient(135deg, #0ea5e9 0%, #2563eb 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 12px 28px;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            margin: 20px 0;
            text-align: center;
        }
        .footer {
            background-color: #f8fafc;
            padding: 24px;
            text-align: center;
            border-top: 1px solid #e4e4e7;
        }
        .footer p {
            color: #71717a;
            font-size: 13px;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>Proposta de Acordo</h1>
        </div>
        <div class="content">
            <h2>Olá, {{ PartyName }}!</h2>
            <p>Você recebeu um acordo de comissão para aceite.</p>
            <div class="details">
                <p><strong>Acordo:</strong> {{ AgreementTitle }}</p>
                <p><strong>Valor total:</strong> {{ Currency }} {{ TotalValue }}</p>
                <p><strong>Sua participação:</strong> {{ SplitPercentage }}%</p>
                <p><strong>Validade:</strong> {{ ExpiresAt }}</p>
                <p><strong>Descrição:</strong> {{ Description }}</p>
                <p><strong>Termos:</strong> {{ Terms }}</p>
            </div>
            <div style="text-align: center;">
                <a href="{{ AcceptUrl }}" class="button">Aceitar proposta</a>
            </div>
            <p style="color: #71717a; font-size: 14px; margin-top: 20px;">
                Se você tiver dúvidas, entre em contato com o responsável pelo acordo.
            </p>
        </div>
        <div class="footer">
            <p><strong>Assistente Executivo</strong></p>
            <p>Este acordo expira em {{ ExpiresAt }}.</p>
        </div>
    </div>
</body>
</html>',
        true,
        NOW()
    );

    -- Template de Lembrete de Aceite (AgreementReminder)
    INSERT INTO "EmailTemplates" ("Id", "Name", "TemplateType", "Subject", "HtmlBody", "IsActive", "CreatedAt")
    VALUES (
        gen_random_uuid(),
        'Acordo - Lembrete de Aceite',
        5, -- AgreementReminder
        'Lembrete: aceite do acordo {{ AgreementTitle }}',
        '<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Lembrete de Aceite</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #18181b;
            background-color: #fff7ed;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #f97316 0%, #f59e0b 100%);
            color: #ffffff;
            padding: 36px 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 26px;
            font-weight: 700;
        }
        .content {
            padding: 36px 30px;
            background-color: #ffffff;
        }
        .content h2 {
            color: #18181b;
            font-size: 20px;
            margin-bottom: 16px;
            font-weight: 600;
        }
        .content p {
            color: #52525b;
            font-size: 16px;
            margin-bottom: 12px;
        }
        .details {
            background-color: #ffedd5;
            padding: 16px;
            border-radius: 6px;
            margin: 20px 0;
        }
        .details p {
            margin-bottom: 8px;
        }
        .button {
            display: inline-block;
            background: linear-gradient(135deg, #f97316 0%, #f59e0b 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 12px 28px;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            margin: 20px 0;
            text-align: center;
        }
        .footer {
            background-color: #fff7ed;
            padding: 24px;
            text-align: center;
            border-top: 1px solid #fed7aa;
        }
        .footer p {
            color: #7c2d12;
            font-size: 13px;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>Lembrete de Aceite</h1>
        </div>
        <div class="content">
            <h2>Olá, {{ PartyName }}!</h2>
            <p>Este é um lembrete para você aceitar o acordo abaixo.</p>
            <div class="details">
                <p><strong>Acordo:</strong> {{ AgreementTitle }}</p>
                <p><strong>Valor total:</strong> {{ Currency }} {{ TotalValue }}</p>
                <p><strong>Sua participação:</strong> {{ SplitPercentage }}%</p>
                <p><strong>Validade:</strong> {{ ExpiresAt }}</p>
            </div>
            <div style="text-align: center;">
                <a href="{{ AcceptUrl }}" class="button">Aceitar proposta</a>
            </div>
            <p style="color: #7c2d12; font-size: 14px; margin-top: 20px;">
                Este acordo expira em {{ ExpiresAt }}.
            </p>
        </div>
        <div class="footer">
            <p><strong>Assistente Executivo</strong></p>
            <p>Se você já aceitou, pode ignorar este lembrete.</p>
        </div>
    </div>
</body>
</html>',
        true,
        NOW()
    );

    -- Template de Acordo Aprovado (AgreementApproved)
    INSERT INTO "EmailTemplates" ("Id", "Name", "TemplateType", "Subject", "HtmlBody", "IsActive", "CreatedAt")
    VALUES (
        gen_random_uuid(),
        'Acordo - Aprovado',
        6, -- AgreementApproved
        'Acordo aprovado: {{ AgreementTitle }}',
        '<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Acordo Aprovado</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #18181b;
            background-color: #ecfdf3;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #16a34a 0%, #22c55e 100%);
            color: #ffffff;
            padding: 36px 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 26px;
            font-weight: 700;
        }
        .content {
            padding: 36px 30px;
            background-color: #ffffff;
        }
        .content h2 {
            color: #18181b;
            font-size: 20px;
            margin-bottom: 16px;
            font-weight: 600;
        }
        .content p {
            color: #52525b;
            font-size: 16px;
            margin-bottom: 12px;
        }
        .details {
            background-color: #dcfce7;
            padding: 16px;
            border-radius: 6px;
            margin: 20px 0;
        }
        .details p {
            margin-bottom: 8px;
        }
        .footer {
            background-color: #ecfdf3;
            padding: 24px;
            text-align: center;
            border-top: 1px solid #bbf7d0;
        }
        .footer p {
            color: #166534;
            font-size: 13px;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>Acordo aprovado</h1>
        </div>
        <div class="content">
            <h2>Olá, {{ PartyName }}!</h2>
            <p>Todos os participantes aprovaram o acordo abaixo.</p>
            <div class="details">
                <p><strong>Acordo:</strong> {{ AgreementTitle }}</p>
                <p><strong>Valor total:</strong> {{ Currency }} {{ TotalValue }}</p>
                <p><strong>Sua participação:</strong> {{ SplitPercentage }}%</p>
                <p><strong>Aprovado em:</strong> {{ ApprovedAt }}</p>
            </div>
            <p style="color: #166534; font-size: 14px; margin-top: 20px;">
                O acordo agora está ativo. Você pode acompanhar os próximos passos no sistema.
            </p>
        </div>
        <div class="footer">
            <p><strong>Assistente Executivo</strong></p>
            <p>Obrigado por confirmar o acordo.</p>
        </div>
    </div>
</body>
</html>',
        true,
        NOW()
    );

    RAISE NOTICE 'Templates de email criados com sucesso!';
END $$;

