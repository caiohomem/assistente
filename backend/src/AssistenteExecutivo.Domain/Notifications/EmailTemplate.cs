using System.Text.RegularExpressions;
using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.ValueObjects;

namespace AssistenteExecutivo.Domain.Notifications;

public class EmailTemplate
{
    private EmailTemplate() { } // EF Core

    public EmailTemplate(
        string name,
        EmailTemplateType templateType,
        string subject,
        string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do template é obrigatório");

        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Assunto do email é obrigatório");

        if (string.IsNullOrWhiteSpace(htmlBody))
            throw new DomainException("Corpo HTML do email é obrigatório");

        Id = Guid.NewGuid();
        Name = name.Trim();
        TemplateType = templateType;
        Subject = subject.Trim();
        HtmlBody = htmlBody;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public EmailTemplateType TemplateType { get; private set; }
    public string Subject { get; private set; }
    public string HtmlBody { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Extrai os campos coringa (placeholders) do template
    /// Ex: {{ NomeUsuario }}, {{ ResetSenhaUrl }}, etc.
    /// </summary>
    public List<string> GetPlaceholders()
    {
        var pattern = @"\{\{\s*(\w+)\s*\}\}";
        var matches = Regex.Matches(HtmlBody + " " + Subject, pattern);
        return matches.Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Valida se todos os campos coringa necessários foram fornecidos
    /// </summary>
    public string ValidatePlaceholders(Dictionary<string, object> values)
    {
        var requiredPlaceholders = GetPlaceholders();
        var missingPlaceholders = requiredPlaceholders
            .Where(p => !values.ContainsKey(p) || values[p] == null || string.IsNullOrWhiteSpace(values[p].ToString()))
            .ToList();

        if (missingPlaceholders.Any())
            return $"Campos obrigatórios faltando: {string.Join(", ", missingPlaceholders)}";

        return string.Empty;
    }

    /// <summary>
    /// Aplica os valores aos campos coringa do template
    /// </summary>
    public string ApplyTemplate(Dictionary<string, object> values)
    {
        var validation = ValidatePlaceholders(values);
        if (!string.IsNullOrWhiteSpace(validation))
            throw new DomainException($"Template de email com campos obrigatórios faltando: {validation}");

        var content = HtmlBody;
        foreach (var value in values)
        {
            var placeholder = $"{{{{ {value.Key} }}}}";
            content = content.Replace(placeholder, value.Value?.ToString() ?? string.Empty);
        }

        return content;
    }

    /// <summary>
    /// Aplica os valores ao assunto do email
    /// </summary>
    public string ApplySubject(Dictionary<string, object> values)
    {
        var subject = Subject;
        foreach (var value in values)
        {
            var placeholder = $"{{{{ {value.Key} }}}}";
            subject = subject.Replace(placeholder, value.Value?.ToString() ?? string.Empty);
        }

        return subject;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Assunto do email é obrigatório");

        if (string.IsNullOrWhiteSpace(htmlBody))
            throw new DomainException("Corpo HTML do email é obrigatório");

        Subject = subject.Trim();
        HtmlBody = htmlBody;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do template é obrigatório");

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum EmailTemplateType
{
    UserCreated = 1,
    PasswordReset = 2,
    Welcome = 3
}

